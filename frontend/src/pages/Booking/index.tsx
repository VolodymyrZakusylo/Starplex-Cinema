import React, { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';
import { useToast } from '@/hooks/useToast';
import api from '@/api/axios';

import { SeatGrid } from './components/SeatGrid';
import { OrderSidebar } from './components/OrderSidebar';
import { History, X, RefreshCw } from 'lucide-react';

import type { SeatMapDto, CashierSaleDto, BookingResponseDto, SignalRSeatsLockedDto, SignalRSeatsReleasedDto } from './types';

const getUserIdFromToken = (): string | null => {
    const token = localStorage.getItem('token');
    if (!token) return null;
    try {
        const base64Url = token.split('.')[1];
        const base64 = base64Url.replace(/-/g, '+').replace(/_/g, '/');
        const jsonPayload = decodeURIComponent(
            window.atob(base64).split('').map((c) => '%' + ('00' + c.charCodeAt(0).toString(16)).slice(-2)).join('')
        );
        const payload = JSON.parse(jsonPayload);
        return payload["http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier"] || payload.sub || null;
    } catch (e) {
        return null;
    }
};

export const BookingPage: React.FC = () => {
    const { sessionId } = useParams<{ sessionId: string }>();
    const navigate = useNavigate();
    const { user } = useAuthStore();
    const { showError, showSuccess } = useToast();

    const isCashierMode = !!user?.roles.some(role => ['Cashier', 'CinemaManager', 'SuperAdmin'].includes(role));

    const [seats, setSeats] = useState<SeatMapDto[]>([]);
    const [selectedSeats, setSelectedSeats] = useState<SeatMapDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    const [cashierPaymentMethod, setCashierPaymentMethod] = useState<'Cash' | 'Card'>('Cash');
    const [lastBookingId, setLastBookingId] = useState<string | null>(null);

    const [isHistoryOpen, setIsHistoryOpen] = useState<boolean>(false);
    const [cashierSales, setCashierSales] = useState<CashierSaleDto[]>([]);
    const [isSalesLoading, setIsSalesLoading] = useState<boolean>(false);

    const [promoCode, setPromoCode] = useState<string>('');
    const [discountPercentage, setDiscountPercentage] = useState<number>(0);
    const [promoError, setPromoError] = useState<string | null>(null);
    const [promoSuccess, setPromoSuccess] = useState<string | null>(null);
    const [isValidatingPromo, setIsValidatingPromo] = useState<boolean>(false);

    const connectionRef = useRef<HubConnection | null>(null);
    const basePrice = 150;
    const currentUserId = getUserIdFromToken();

    const fetchSeatMap = async () => {
        if (!sessionId) return;
        setIsLoading(true);
        try {
            const response = await api.get<SeatMapDto[]>(`/Bookings/session/${sessionId}/seats`);
            setSeats(response.data);

            if (currentUserId) {
                const myLockedSeats = response.data.filter(
                    (s) => s.status === 'Locked' && s.lockedByUserId?.toLowerCase() === currentUserId.toLowerCase()
                );
                setSelectedSeats(myLockedSeats);
            }
        } catch (err) {
            showError('Не вдалося синхронізувати схему залу.');
        } finally {
            setIsLoading(false);
        }
    };

    const fetchCashierSales = async () => {
        if (!isCashierMode) return;
        setIsSalesLoading(true);
        try {
            const response = await api.get<CashierSaleDto[]>('/Bookings/cashier-sales');
            setCashierSales(response.data);
        } catch (err) {
            showError('Не вдалося отримати лог касової зміни.');
        } finally {
            setIsSalesLoading(false);
        }
    };

    useEffect(() => {
        fetchSeatMap();
    }, [sessionId, currentUserId]);

    useEffect(() => {
        if (isHistoryOpen) fetchCashierSales();
    }, [isHistoryOpen]);

    useEffect(() => {
        if (!sessionId || connectionRef.current) return;

        let isMounted = true;
        const connection = new HubConnectionBuilder()
            .withUrl('/hub/seats')
            .withAutomaticReconnect()
            .configureLogging(LogLevel.Information)
            .build();

        const startConnection = async () => {
            try {
                await connection.start();
                if (!isMounted) {
                    await connection.stop();
                    return;
                }
                connectionRef.current = connection;
                await connection.invoke('JoinSessionRoom', sessionId);

                const handleSeatsLocked = (data: SignalRSeatsLockedDto) => {
                    if (!data?.seatIds) return;
                    setSeats((prev) => prev.map((s) => data.seatIds.includes(s.seatId) ? { ...s, status: 'Locked', lockedByUserId: data.lockedByUserId } : s));
                };

                const handleSeatsReleased = (data: SignalRSeatsReleasedDto) => {
                    if (!data?.seatIds) return;
                    setSeats((prev) => prev.map((s) => data.seatIds.includes(s.seatId) ? { ...s, status: 'Available', lockedByUserId: null } : s));
                    setSelectedSeats((prev) => prev.filter((s) => !data.seatIds.includes(s.seatId)));
                };

                connection.on('seatsLocked', handleSeatsLocked);
                connection.on('SeatsLocked', handleSeatsLocked);
                connection.on('seatsReleased', handleSeatsReleased);
                connection.on('SeatsReleased', handleSeatsReleased);
            } catch (err) {
                console.error(err);
            }
        };

        startConnection();
        return () => {
            isMounted = false;
            if (connectionRef.current?.state === HubConnectionState.Connected) {
                connectionRef.current.invoke('LeaveSessionRoom', sessionId).then(() => connectionRef.current?.stop());
            }
        };
    }, [sessionId]);

    const handleSeatClick = async (seat: SeatMapDto) => {
        const isSelected = selectedSeats.some((s) => s.seatId === seat.seatId);
        try {
            const url = isSelected ? '/Bookings/unlock' : '/Bookings/lock';
            const response = await api.post<{ isSuccess: boolean }>(url, { sessionId, seatIds: [seat.seatId] });
            
            if (response.data.isSuccess) {
                if (isSelected) {
                    setSelectedSeats((p) => p.filter((s) => s.seatId !== seat.seatId));
                    setSeats((p) => p.map((s) => s.seatId === seat.seatId ? { ...s, status: 'Available', lockedByUserId: null } : s));
                } else {
                    setSelectedSeats((p) => [...p, seat]);
                    setSeats((p) => p.map((s) => s.seatId === seat.seatId ? { ...s, status: 'Locked', lockedByUserId: currentUserId } : s));
                }
            }
        } catch (err: any) {
            showError(err.response?.data?.Message || 'Місце вже заблоковане іншим користувачем.');
        }
    };

    const handleValidatePromo = async () => {
        if (!promoCode.trim()) return;
        setIsValidatingPromo(true);
        setPromoError(null);
        setPromoSuccess(null);
        try {
            const res = await api.get<{ percentage: number }>(`/Discounts/validate/${promoCode.trim()}`);
            setDiscountPercentage(res.data.percentage);
            setPromoSuccess(`Активувано знижку ${res.data.percentage}%`);
        } catch (err: any) {
            setPromoError(err.response?.data?.message || 'Промокод не діє.');
        } finally {
            setIsValidatingPromo(false);
        }
    };

    const handleCreateBooking = async () => {
        if (selectedSeats.length === 0 || !sessionId) return;
        setIsSubmitting(true);
        try {
            const res = await api.post<BookingResponseDto>('/Bookings/create', {
                sessionId,
                seatIds: selectedSeats.map((s) => s.seatId),
                promoCode: discountPercentage > 0 ? promoCode.trim() : undefined
            });
            navigate(`/booking/payment?clientSecret=${res.data.clientSecret}`, { state: res.data });
        } catch (err: any) {
            showError(err.response?.data?.Message || 'Не вдалося створити квитки.');
        } finally {
            setIsSubmitting(false);
        }
    };

    const triggerTicketPrint = async (id: string) => {
        try {
            const res = await api.get(`/Bookings/${id}/tickets/download-all`, { responseType: 'blob' });
            const iframe = document.createElement('iframe');
            iframe.style.position = 'fixed'; iframe.style.top = '-10000px';
            iframe.src = window.URL.createObjectURL(new Blob([res.data], { type: 'application/pdf' }));
            iframe.onload = () => { iframe.contentWindow?.print(); };
            document.body.appendChild(iframe);
        } catch {
            showError('Помилка термодруку квитків.');
        }
    };

    const handleProcessCashierSale = async () => {
        if (selectedSeats.length === 0 || !sessionId) return;
        setIsSubmitting(true);
        try {
            const res = await api.post<string>('/Bookings/cashier-sell', { sessionId, seatIds: selectedSeats.map((s) => s.seatId), paymentMethod: cashierPaymentMethod });
            setLastBookingId(res.data);
            await triggerTicketPrint(res.data);
            setSelectedSeats([]);
            showSuccess('Касовий POS-продаж успішно завершено!');
            fetchSeatMap();
        } catch {
            showError('Помилка реєстрації чеку каси.');
        } finally {
            setIsSubmitting(false);
        }
    };

    if (isLoading) {
        return (
            <div className="flex flex-col items-center justify-center py-40 text-white">
                <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    return (
        <div className="w-full select-none grid grid-cols-1 lg:grid-cols-4 gap-8 items-start px-2 py-4">
            <SeatGrid seats={seats} selectedSeats={selectedSeats} currentUserId={currentUserId} isCashierMode={isCashierMode} onSeatClick={handleSeatClick} onOpenHistory={() => setIsHistoryOpen(true)} isSalesLoading={isSalesLoading} />
            <OrderSidebar selectedSeats={selectedSeats} basePrice={basePrice} isCashierMode={isCashierMode} promoCode={promoCode} setPromoCode={setPromoCode} promoError={promoError} promoSuccess={promoSuccess} isValidatingPromo={isValidatingPromo} discountPercentage={discountPercentage} cashierPaymentMethod={cashierPaymentMethod} setCashierPaymentMethod={setCashierPaymentMethod} isSubmitting={isSubmitting} lastBookingId={lastBookingId} onValidatePromo={handleValidatePromo} onCreateBooking={handleCreateBooking} onProcessCashierSale={handleProcessCashierSale} onTriggerPrint={triggerTicketPrint} />
        </div>
    );
};