import React, { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { Ticket, ShieldAlert, User, UserMinus } from 'lucide-react';
import api from '@/api/axios';

import { ProfileDataForm } from './components/ProfileDataForm';
import { ChangePasswordForm } from './components/ChangePasswordForm';
import { BookingCard } from './components/BookingCard';

interface UserTicketDto {
    ticketId: string;
    ticketCode: string;
    row: number;
    seatNumber: number;
    seatType: number;
}

interface UserBookingDto {
    id: string;
    bookingDate: string;
    movieTitle: string;
    movieImageUrl: string;
    sessionStartTime: string;
    hallName: string;
    totalPrice: number;
    tickets: UserTicketDto[];
    isPast: boolean;
}

export const UserProfilePage: React.FC = () => {
    const [bookings, setBookings] = useState<UserBookingDto[]>([]);
    const [isLoading, setIsLoading] = useState(true);
    const [activeTab, setActiveTab] = useState<'tickets' | 'settings'>('tickets');
    
    const [userSession, setUserSession] = useState<{ firstName: string; lastName: string } | null>(null);
    const [downloadingId, setDownloadingId] = useState<string | null>(null);
    const [cancellingTicketId, setCancellingTicketId] = useState<string | null>(null);

    const fetchMyBookings = async () => {
        try {
            const response = await api.get<UserBookingDto[]>('/Bookings/my-bookings');
            setBookings(response.data);
            
            const storedUser = localStorage.getItem('user');
            if (storedUser) {
                setUserSession(JSON.parse(storedUser));
            }
        } catch (err) {
            console.error('Не вдалося завантажити історію замовлень:', err);
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchMyBookings();
    }, []);

    const handleDeleteAccount = async () => {
        const firstConfirm = window.confirm('⚠️ УВАГА! Ви дійсно хочете видалити аккаунт? Цю дію неможливо скасувати.');
        if (!firstConfirm) return;

        const secondConfirm = window.prompt('Введіть "ВИДАЛИТИ" для підтвердження:');
        if (secondConfirm !== 'ВИДАЛИТИ') return;

        try {
            await api.delete('/User/delete-account');
            alert('Аккаунт успішно видалено.');
            localStorage.clear();
            window.location.href = '/';
        } catch (err) {
            alert('Не вдалося видалити аккаунт.');
        }
    };

    const handleDownloadAll = async (bookingId: string) => {
        setDownloadingId(bookingId);
        try {
            const response = await api.get(`/Bookings/${bookingId}/tickets/download-all`, { responseType: 'blob' });
            const blob = new Blob([response.data], { type: 'application/pdf' });
            const downloadUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = downloadUrl;
            link.setAttribute('download', `StarPlex-Booking-${bookingId.substring(0, 8).toUpperCase()}.pdf`);
            document.body.appendChild(link);
            link.click();
            link.parentNode?.removeChild(link);
            window.URL.revokeObjectURL(downloadUrl);
        } catch (err) {
            alert('Не вдалося завантажити квитки.');
        } finally {
            setDownloadingId(null);
        }
    };

    const handleCancelTicket = async (ticketId: string) => {
        if (!window.confirm('Ви впевнені, що хочете повернути цей квиток?')) return;
        setCancellingTicketId(ticketId);
        try {
            await api.post(`/Bookings/tickets/${ticketId}/cancel`);
            alert('Квиток скасовано, кошти повернено!');
            await fetchMyBookings();
        } catch (err: any) {
            alert(err.response?.data?.message || 'Не вдалося скасувати квиток.');
        } finally {
            setCancellingTicketId(null);
        }
    };

    if (isLoading) {
        return (
            <div className="min-h-screen bg-dark-bg text-white flex items-center justify-center">
                <div className="w-8 h-8 border-4 border-accent-gold border-t-transparent rounded-full animate-spin"></div>
            </div>
        );
    }

    const upcomingBookings = bookings.filter((b) => !b.isPast && b.tickets.length > 0);
    const pastBookings = bookings.filter((b) => b.isPast || b.tickets.length === 0);

    return (
        <div className="w-full min-h-screen bg-dark-bg text-white py-12 px-4 sm:px-6 lg:px-8 select-none animate-fadeIn">
            <div className="max-w-4xl mx-auto">
                
                <div className="border-b border-white/5 pb-6 mb-8 flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4">
                    <div>
                        <h1 className="text-3xl font-black tracking-tight">Мій профіль</h1>
                        <p className="text-text-muted text-sm mt-1">Історія замовлень кінотеатру та налаштування безпеки</p>
                    </div>
                    <Link to="/" className="text-xs bg-white/5 hover:bg-white/10 border border-white/10 px-4 py-2 rounded-xl font-medium transition-all uppercase tracking-wider">
                        На головну
                    </Link>
                </div>

                <div className="flex gap-2 border-b border-white/5 pb-4 mb-8 text-xs font-bold uppercase tracking-wider">
                    <button onClick={() => setActiveTab('tickets')} className={`flex items-center gap-2 px-4 py-2.5 rounded-xl border transition-all cursor-pointer ${activeTab === 'tickets' ? 'bg-accent-gold border-accent-gold text-dark-bg' : 'bg-transparent border-white/5 text-gray-400 hover:text-white'}`}>
                        <Ticket className="w-4 h-4" /> Мої квитки
                    </button>
                    <button onClick={() => setActiveTab('settings')} className={`flex items-center gap-2 px-4 py-2.5 rounded-xl border transition-all cursor-pointer ${activeTab === 'settings' ? 'bg-accent-gold border-accent-gold text-dark-bg' : 'bg-transparent border-white/5 text-gray-400 hover:text-white'}`}>
                        <User className="w-4 h-4" /> Налаштування аккаунта
                    </button>
                </div>

                {activeTab === 'tickets' ? (
                    <div className="flex flex-col gap-10">
                        <div>
                            <h2 className="text-lg font-black tracking-tight text-accent-gold mb-4 uppercase tracking-wider">🍿 Найближчі перегляди ({upcomingBookings.length})</h2>
                            {upcomingBookings.length === 0 ? (
                                <div className="bg-dark-secondary border border-white/5 rounded-2xl p-8 text-center text-text-muted text-xs">
                                    У вас немає активних квитків. <Link to="/" className="text-accent-gold hover:underline font-bold mt-2 inline-block">Придбати квитки 🎟️</Link>
                                </div>
                            ) : (
                                <div className="space-y-4">
                                    {upcomingBookings.map((booking) => (
                                        <BookingCard key={booking.id} booking={booking} onDownload={handleDownloadAll} downloadingId={downloadingId} onCancelTicket={handleCancelTicket} cancellingTicketId={cancellingTicketId} />
                                    ))}
                                </div>
                            )}
                        </div>

                        <div>
                            <h2 className="text-md font-bold text-text-muted mb-4 uppercase tracking-wider">🎬 Архів замовлень або скасовані квитки</h2>
                            {pastBookings.length === 0 ? (
                                <div className="text-text-muted text-xs italic pl-2">Історія архіву порожня.</div>
                            ) : (
                                <div className="space-y-4 opacity-60 hover:opacity-100 transition-opacity">
                                    {pastBookings.map((booking) => (
                                        <BookingCard key={booking.id} booking={booking} onDownload={handleDownloadAll} downloadingId={downloadingId} isPast onCancelTicket={handleCancelTicket} cancellingTicketId={cancellingTicketId} />
                                    ))}
                                </div>
                            )}
                        </div>
                    </div>
                ) : (
                    <div className="grid grid-cols-1 md:grid-cols-2 gap-8 items-start text-xs">
                        
                        <ProfileDataForm initialFirstName={userSession?.firstName || ''} initialLastName={userSession?.lastName || ''} />

                        <ChangePasswordForm />

                        <div className="md:col-span-2 bg-red-950/10 border border-red-500/20 p-6 rounded-2xl flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mt-4">
                            <div className="flex gap-3 items-start">
                                <ShieldAlert className="w-5 h-5 text-red-400 shrink-0 mt-0.5" />
                                <div>
                                    <h4 className="text-sm font-bold text-red-400 uppercase tracking-wider">Видалення облікового запису</h4>
                                    <p className="text-text-muted text-[11px] mt-0.5">Повне очищення історії відвідувань сеансів StarPlex та анулювання квитків.</p>
                                </div>
                            </div>
                            <button type="button" onClick={handleDeleteAccount} className="bg-red-500/10 hover:bg-red-600 border border-red-500/20 hover:border-red-600 text-red-400 hover:text-white px-4 py-2.5 rounded-xl font-bold transition-all uppercase tracking-wider shrink-0 cursor-pointer">
                                <UserMinus className="w-4 h-4 inline mr-1.5" /> Видалити аккаунт
                            </button>
                        </div>
                    </div>
                )}
            </div>
        </div>
    );
};

export default UserProfilePage;