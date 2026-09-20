import React, { useEffect, useState, useRef } from 'react';
import { useParams, useNavigate, useLocation } from 'react-router-dom';
import { HubConnection, HubConnectionBuilder, LogLevel, HubConnectionState } from '@microsoft/signalr';
import { useAuthStore } from '@/store/authStore';
import { useToast } from '@/hooks/useToast';
import { bookingsApi } from '@/api/bookings';
import { sessionsApi } from '@/api/sessions';
import { discountsApi } from '@/api/discounts';

import { SeatGrid } from './components/SeatGrid';
import { OrderSidebar } from './components/OrderSidebar';

import type { SeatMapDto, CashierSaleDto, SignalRSeatsLockedDto, SignalRSeatsReleasedDto } from '@/types/bookings';

interface BookingLocationState {
    basePrice?: number;
}

export const BookingPage: React.FC = () => {
    const { sessionId } = useParams<{ sessionId: string }>();
    const navigate = useNavigate();
    const location = useLocation();
    const locationState = location.state as BookingLocationState | null;

    const { user } = useAuthStore();
    const { showError, showSuccess } = useToast();

    const isCashierMode = !!user?.roles.some(role => ['Cashier', 'CinemaManager', 'SuperAdmin'].includes(role));

    const [seats, setSeats] = useState<SeatMapDto[]>([]);
    const [selectedSeats, setSelectedSeats] = useState<SeatMapDto[]>([]);
    const [basePrice, setBasePrice] = useState<number | null>(
        locationState?.basePrice ?? null
    );
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    const [cashierPaymentMethod, setCashierPaymentMethod] = useState<'Cash' | 'Card'>('Cash');
    const [lastBookingId, setLastBookingId] = useState<string | null>(null);

    const [isHistoryOpen, setIsHistoryOpen] = useState<boolean>(false);
    const [, setCashierSales] = useState<CashierSaleDto[]>([]);
    const [isSalesLoading, setIsSalesLoading] = useState<boolean>(false);

    const [promoCode, setPromoCode] = useState<string>('');
    const [discountPercentage, setDiscountPercentage] = useState<number>(0);
    const [promoError, setPromoError] = useState<string | null>(null);
    const [promoSuccess, setPromoSuccess] = useState<string | null>(null);
    const [isValidatingPromo, setIsValidatingPromo] = useState<boolean>(false);

    const connectionRef = useRef<HubConnection | null>(null);
    const currentUserId = user?.userId || null;

    const fetchSeatMap = async () => {
        if (!sessionId) return;
        setIsLoading(true);
        try {
            const data = await bookingsApi.getSeats(sessionId);
            setSeats(data);

            if (currentUserId) {
                const myLockedSeats = data.filter(
                    (s) => s.status === 'Locked' && s.lockedByUserId?.toLowerCase() === currentUserId.toLowerCase()
                );
                setSelectedSeats(myLockedSeats);
            }

            if (basePrice === null) {
                const allSessions = await sessionsApi.getAll();
                const currentSession = allSessions.find((s) => s.id === sessionId);
                if (currentSession?.basePrice !== undefined && currentSession?.basePrice !== null) {
                    setBasePrice(currentSession.basePrice);
                }
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
            const data = await bookingsApi.getCashierSales();
            setCashierSales(data);
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
            .withUrl('/hub/seats', {
                accessTokenFactory: () => localStorage.getItem('token') ?? '',
            })
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
                    setSeats((prev) => prev.map((s) => data.seatIds.includes(s.seatId) ? { ...s, status: 'Available', lockedByUserId: undefined } : s));
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

    const pendingSeatIdsRef = useRef<Set<string>>(new Set());

    const handleSeatClick = async (seat: SeatMapDto) => {
        if (pendingSeatIdsRef.current.has(seat.seatId)) return;
        pendingSeatIdsRef.current.add(seat.seatId);

        const isSelected = selectedSeats.some((s) => s.seatId === seat.seatId);
        try {
            const isSuccess = isSelected 
                ? await bookingsApi.unlockSeat(sessionId!, seat.seatId)
                : await bookingsApi.lockSeat(sessionId!, seat.seatId);
            
            if (isSuccess) {
                if (isSelected) {
                    setSelectedSeats((p) => p.filter((s) => s.seatId !== seat.seatId));
                    setSeats((p) => p.map((s) => s.seatId === seat.seatId ? { ...s, status: 'Available', lockedByUserId: undefined } : s));
                } else {
                    setSelectedSeats((p) => [...p, seat]);
                    setSeats((p) => p.map((s) => s.seatId === seat.seatId ? { ...s, status: 'Locked', lockedByUserId: currentUserId || undefined } : s));
                }
            }
        } catch (err: any) {
            const status = err.response?.status;
            const backendMessage = err.response?.data?.detail || err.response?.data?.message || err.response?.data?.Message;
            if (status === 409) {
                showError(backendMessage || 'Місце вже заблоковане іншим користувачем.');
            } else if (backendMessage) {
                showError(backendMessage);
            } else {
                showError(isSelected ? 'Не вдалося розблокувати місце.' : 'Не вдалося заблокувати місце.');
            }
        } finally {
            pendingSeatIdsRef.current.delete(seat.seatId);
        }
    };

    const handleValidatePromo = async () => {
        if (!promoCode.trim()) return;
        setIsValidatingPromo(true);
        setPromoError(null);
        setPromoSuccess(null);
        try {
            const res = await discountsApi.validate(promoCode.trim());
            setDiscountPercentage(res.percentage);
            setPromoSuccess(`Активувано знижку ${res.percentage}%`);
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
            const res = await bookingsApi.create(
                sessionId,
                selectedSeats.map((s) => s.seatId),
                discountPercentage > 0 ? promoCode.trim() : undefined
            );
            navigate(`/booking/payment?clientSecret=${res.clientSecret}`, { state: res });
        } catch (err: any) {
            showError(err.response?.data?.Message || 'Не вдалося створити квитки.');
        } finally {
            setIsSubmitting(false);
        }
    };

    const triggerTicketPrint = async (id: string) => {
        try {
            const blobData = await bookingsApi.downloadTickets(id);
            const iframe = document.createElement('iframe');
            iframe.style.position = 'fixed'; iframe.style.top = '-10000px';
            iframe.src = window.URL.createObjectURL(new Blob([blobData], { type: 'application/pdf' }));
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
            const bookingId = await bookingsApi.cashierSell(sessionId, selectedSeats.map((s) => s.seatId), cashierPaymentMethod);
            setLastBookingId(bookingId);
            await triggerTicketPrint(bookingId);
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

    if (basePrice === null) {
        return (
            <div className="w-full flex flex-col items-center justify-center py-32 text-white text-center">
                <div className="bg-[#1a1c26] border border-white/10 p-8 rounded-3xl max-w-md w-full shadow-2xl flex flex-col items-center gap-4">
                    <p className="text-sm font-bold text-gray-300">Не вдалося завантажити ціну сеансу.</p>
                    <button
                        type="button"
                        onClick={() => navigate('/')}
                        className="px-6 py-3 bg-[#ffbd14] text-black font-black text-xs uppercase tracking-wider rounded-xl border-none cursor-pointer hover:bg-[#e0a410] transition-all"
                    >
                        Повернутися на головну
                    </button>
                </div>
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