import React from 'react';
import { Download } from 'lucide-react';
import type { UserBookingDto } from '@/types/bookings';

interface BookingCardProps {
    booking: UserBookingDto;
    onDownload: (id: string) => void;
    downloadingId: string | null;
    onCancelTicket: (id: string) => void;
    cancellingTicketId: string | null;
    isPast?: boolean;
}

export const BookingCard: React.FC<BookingCardProps> = ({ 
    booking, onDownload, downloadingId, onCancelTicket, cancellingTicketId, isPast 
}) => {
    const sessionTime = (booking as any).sessionStartTime || (booking as any).sessionTime;
    const formattedDate = sessionTime ? new Date(sessionTime).toLocaleString('uk-UA', {
        day: 'numeric', 
        month: 'long', 
        year: 'numeric', 
        hour: '2-digit', 
        minute: '2-digit',
        timeZone: 'Europe/Kyiv'
    }) : '';

    const movieImageUrl = (booking as any).movieImageUrl || (booking as any).moviePosterUrl;
    const movieTitle = (booking as any).movieTitle || 'Фільм';
    const hallName = (booking as any).hallName || 'Зал';

    return (
        <div className="bg-dark-secondary border border-white/5 rounded-2xl p-6 flex flex-col md:flex-row gap-6 items-start md:items-center justify-between shadow-xl relative overflow-hidden">
            <div className="flex gap-4 items-start w-full">
                <div className="flex-shrink-0">
                    {movieImageUrl ? (
                        <img src={movieImageUrl} alt={movieTitle} className="w-16 h-24 object-cover rounded-xl border border-white/10 shadow-md" onError={(e) => { (e.target as HTMLElement).style.display = 'none'; }} />
                    ) : (
                        <div className="w-16 h-24 bg-dark-bg border border-white/10 rounded-xl flex items-center justify-center text-2xl shadow-inner text-white/20">🎬</div>
                    )}
                </div>
                <div className="flex-grow">
                    <h3 className="text-lg font-bold text-white tracking-tight">{movieTitle}</h3>
                    <p className="text-accent-gold text-sm font-medium mt-1">{formattedDate}</p>
                    <p className="text-text-muted text-xs mt-1">🏛 {hallName} · № {booking.id.substring(0, 8).toUpperCase()}</p>

                    <div className="flex flex-wrap gap-2 mt-4">
                        {!booking.tickets || booking.tickets.length === 0 ? (
                            <span className="text-[10px] uppercase tracking-wider text-red-400 font-bold bg-red-500/10 border border-red-500/20 px-2 py-0.5 rounded-full">Усі квитки скасовано (Повернення)</span>
                        ) : (
                            booking.tickets.map((t: any) => {
                                const ticketId = t.ticketId || t.id;
                                const isThisCancelling = cancellingTicketId === ticketId;
                                return (
                                    <div key={ticketId} className="group flex items-center bg-dark-bg border border-white/5 pl-2.5 pr-1.5 py-1 rounded-xl text-[11px] text-text-muted hover:border-red-500/30 hover:text-white transition-all">
                                        <span>Ряд {t.row}, Місце {t.seatNumber}</span>
                                        {!isPast && (
                                            <button
                                                onClick={() => onCancelTicket(ticketId)}
                                                disabled={cancellingTicketId !== null}
                                                className="ml-2 p-1 text-gray-500 hover:text-red-400 transition-colors disabled:cursor-not-allowed border-none bg-transparent cursor-pointer"
                                            >
                                                {isThisCancelling ? <div className="w-3 h-3 border border-red-400 border-t-transparent rounded-full animate-spin"></div> : '✕'}
                                            </button>
                                        )}
                                    </div>
                                );
                            })
                        )}
                    </div>
                </div>
            </div>

            <div className="w-full md:w-auto flex flex-row md:flex-col justify-between md:justify-center items-center md:items-end gap-3 border-t md:border-t-0 border-white/5 pt-4 md:pt-0 shrink-0">
                <div className="text-left md:text-right">
                    <span className="text-[10px] text-text-muted uppercase tracking-wider block">Сума платежу</span>
                    <span className="text-xl font-black text-accent-gold">{booking.totalPrice.toFixed(2)} ₴</span>
                </div>

                {!isPast && booking.tickets && booking.tickets.length > 0 && (
                    <button onClick={() => onDownload(booking.id)} disabled={downloadingId !== null || cancellingTicketId !== null} className="bg-white/5 border border-white/10 hover:border-accent-gold text-white hover:text-dark-bg font-bold text-xs px-4 py-2 rounded-xl transition-all flex items-center gap-1.5 disabled:opacity-50 disabled:cursor-not-allowed cursor-pointer">
                        <Download className="w-3.5 h-3.5" /> PDF квитки
                    </button>
                )}
            </div>
        </div>
    );
};