import React, { useState } from 'react';
import { useLocation, Link } from 'react-router-dom';
import { CheckCircle2, Download, Home, AlertCircle } from 'lucide-react';
import { bookingsApi } from '@/api/bookings';

export const BookingSuccessPage: React.FC = () => {
    const location = useLocation();

    const [isDownloading, setIsDownloading] = useState<boolean>(false);
    const [downloadError, setDownloadError] = useState<string | null>(null);
    const { bookingId } = (location.state as { bookingId: string }) || {};

    if (!bookingId) {
        return (
            <div className="text-center py-40 text-white flex flex-col items-center justify-center gap-4">
                <AlertCircle className="w-12 h-12 text-red-500 animate-pulse" />
                <p className="text-red-400 font-bold">Критична помилка: Ідентифікатор замовлення відсутній.</p>
                <Link to="/" className="bg-white/5 border border-white/10 px-5 py-2.5 rounded-xl text-xs font-bold uppercase tracking-wider text-white hover:bg-white/10 transition-all">
                    На головну
                </Link>
            </div>
        );
    }

    const handleDownloadTickets = async () => {
        setIsDownloading(true);
        setDownloadError(null);
        try {
            const blobData = await bookingsApi.downloadTickets(bookingId);
            const blob = new Blob([blobData], { type: 'application/pdf' });
            const downloadUrl = window.URL.createObjectURL(blob);
            const link = document.createElement('a');
            link.href = downloadUrl;
            
            link.setAttribute('download', `StarPlex-Tickets-${bookingId.substring(0, 8).toUpperCase()}.pdf`);
            document.body.appendChild(link);
            link.click();
            
            link.parentNode?.removeChild(link);
            window.URL.revokeObjectURL(downloadUrl);

        } catch (err) {
            console.error('Помилка завантаження квитків:', err);
            setDownloadError('Не вдалося завантажити PDF локально. Квитки вже надіслано на вашу електронну пошту!');
        } finally {
            setIsDownloading(false);
        }
    };

    return (
        <div className="w-full flex items-center justify-center select-none py-12 px-4">
            <div className="w-full max-w-md bg-[#1a1c26] border border-white/5 p-8 rounded-2xl shadow-2xl text-center animate-fadeIn">
                
                {/* Анімований індикатор успіху */}
                <div className="w-20 h-20 bg-emerald-500/10 border border-emerald-500/30 rounded-full flex items-center justify-center mx-auto mb-6 shadow-lg shadow-emerald-500/5">
                    <CheckCircle2 className="w-10 h-10 text-emerald-400" />
                </div>

                <h2 className="text-2xl font-black tracking-tight text-white">Оплата успішна!</h2>
                <p className="text-gray-400 text-xs mt-2 leading-relaxed">
                    Ваші квитки успішно сформовані. На вашу електронну пошту відправлено лист із деталями замовлення та вбудованими PDF-файлами.
                </p>

                <div className="bg-[#111219]/50 border border-white/5 p-4 rounded-xl my-8 text-left text-xs space-y-3">
                    <div className="flex justify-between items-center">
                        <span className="text-gray-500 font-medium">Статус платежу:</span>
                        <span className="text-emerald-400 font-black uppercase text-[10px] tracking-widest bg-emerald-500/10 px-2 py-0.5 rounded-md border border-emerald-500/20">
                            Сплачено
                        </span>
                    </div>
                    <div className="flex justify-between items-center">
                        <span className="text-gray-500 font-medium">Номер замовлення:</span>
                        <span className="font-mono font-bold text-[#ffbd14] tracking-wide">
                            #{bookingId.substring(0, 8).toUpperCase()}
                        </span>
                    </div>
                </div>

                {downloadError && (
                    <div className="p-3 bg-amber-500/10 border border-amber-500/20 text-amber-400 text-xs font-medium rounded-xl flex items-center gap-2 mb-4 text-left leading-relaxed">
                        <div className="text-left">{downloadError}</div>
                    </div>
                )}

                <div className="flex flex-col gap-3 text-xs">
                    <button
                        type="button"
                        onClick={handleDownloadTickets}
                        disabled={isDownloading}
                        className="w-full bg-[#ffbd14] hover:bg-[#e0a40f] disabled:bg-white/5 text-black disabled:text-gray-500 font-black py-3.5 rounded-xl transition-all shadow-lg flex items-center justify-center gap-2 disabled:cursor-not-allowed border-none cursor-pointer uppercase tracking-wider"
                    >
                        {isDownloading ? (
                            <>
                                <div className="w-4 h-4 border-2 border-black border-t-transparent rounded-full animate-spin"></div>
                                <span>Завантаження квитків...</span>
                            </>
                        ) : (
                            <>
                                <Download className="w-4 h-4" />
                                <span>Завантажити квитки (PDF)</span>
                            </>
                        )}
                    </button>
                    
                    <Link
                        to="/"
                        className="w-full bg-white/5 hover:bg-white/10 border border-white/10 text-white font-bold py-3.5 rounded-xl transition-all block text-center no-underline uppercase tracking-wider box-border"
                    >
                        <span className="flex items-center justify-center gap-2">
                            <Home className="w-4 h-4" /> Повернутися на головну
                        </span>
                    </Link>
                </div>

                <p className="text-[10px] text-center text-gray-500 mt-8 uppercase tracking-widest font-bold">
                    Дякуємо, що обираєте StarPlex! Приємного перегляду 🍿
                </p>
            </div>
        </div>
    );
};

export default BookingSuccessPage;