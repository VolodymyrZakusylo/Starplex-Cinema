import React, { useEffect, useRef } from 'react';
import { X, Clock, Banknote, CreditCard, AlertCircle } from 'lucide-react';
import type { CashierSaleDto } from '@/types/bookings';

interface CashierHistoryModalProps {
    isOpen: boolean;
    onClose: () => void;
    sales: CashierSaleDto[];
    isLoading: boolean;
    error: string | null;
    onRetry: () => void;
}

export const CashierHistoryModal: React.FC<CashierHistoryModalProps> = ({
    isOpen,
    onClose,
    sales,
    isLoading,
    error,
    onRetry
}) => {
    const previousFocusRef = useRef<HTMLElement | null>(null);
    const modalRef = useRef<HTMLDivElement>(null);

    useEffect(() => {
        const handleKeyDown = (e: KeyboardEvent) => {
            if (e.key === 'Escape') onClose();
            if (e.key === 'Tab' && modalRef.current) {
                const focusableElements = modalRef.current.querySelectorAll<HTMLElement>(
                    'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
                );
                if (focusableElements.length === 0) return;
                const firstElement = focusableElements[0];
                const lastElement = focusableElements[focusableElements.length - 1];

                if (e.shiftKey) {
                    if (document.activeElement === firstElement) {
                        lastElement?.focus();
                        e.preventDefault();
                    }
                } else {
                    if (document.activeElement === lastElement) {
                        firstElement?.focus();
                        e.preventDefault();
                    }
                }
            }
        };

        if (isOpen) {
            previousFocusRef.current = document.activeElement as HTMLElement;
            const previousOverflow = document.body.style.overflow;
            document.body.style.overflow = 'hidden';
            window.addEventListener('keydown', handleKeyDown);

            setTimeout(() => {
                const closeBtn = modalRef.current?.querySelector('button[aria-label="Закрити"]') as HTMLButtonElement;
                closeBtn?.focus();
            }, 0);

            return () => {
                window.removeEventListener('keydown', handleKeyDown);
                document.body.style.overflow = previousOverflow;
                if (previousFocusRef.current) {
                    previousFocusRef.current.focus();
                }
            };
        }
    }, [isOpen, onClose]);

    if (!isOpen) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
            <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" onClick={onClose} />
            <div
                ref={modalRef}
                role="dialog"
                aria-modal="true"
                aria-labelledby="modal-title"
                className="relative bg-[#1a1c26] border border-purple-500/30 rounded-3xl shadow-2xl w-full max-w-3xl max-h-[85vh] flex flex-col overflow-hidden animate-fadeIn"
            >
                <div className="flex items-center justify-between p-6 border-b border-white/5 bg-[#141622]">
                    <div>
                        <h2 id="modal-title" className="text-xl font-black text-white uppercase tracking-wider">Журнал каси</h2>
                        <p className="text-xs text-gray-400 mt-1">Останні продажі за поточну зміну</p>
                    </div>
                    <button
                        onClick={onClose}
                        className="p-2 hover:bg-white/10 rounded-full transition-colors text-gray-400 hover:text-white cursor-pointer bg-transparent border-none"
                        aria-label="Закрити"
                    >
                        <X className="w-5 h-5" />
                    </button>
                </div>

                <div className="flex-1 overflow-y-auto p-6">
                    {isLoading ? (
                        <div className="flex flex-col items-center justify-center py-20 text-purple-400">
                            <div className="w-8 h-8 border-4 border-current border-t-transparent rounded-full animate-spin mb-4" />
                            <p className="text-sm font-bold">Завантаження даних...</p>
                        </div>
                    ) : error ? (
                        <div className="flex flex-col items-center justify-center py-20 text-red-400">
                            <AlertCircle className="w-10 h-10 mb-4" />
                            <p className="text-sm font-bold mb-4">{error}</p>
                            <button onClick={onRetry} className="px-4 py-2 bg-white/5 hover:bg-white/10 rounded-xl text-xs font-semibold cursor-pointer border-none transition-colors text-white">Спробувати знову</button>
                        </div>
                    ) : sales.length === 0 ? (
                        <div className="text-center py-20 text-gray-400">
                            <p className="text-sm font-bold">Продажів ще не було</p>
                            <button onClick={onRetry} className="mt-4 px-4 py-2 bg-white/5 hover:bg-white/10 rounded-xl text-xs font-semibold cursor-pointer border-none transition-colors">Оновити</button>
                        </div>
                    ) : (
                        <div className="flex flex-col gap-4">
                            {sales.map(sale => (
                                <div key={sale.bookingId} className="bg-[#111219]/50 border border-white/5 p-4 rounded-2xl flex flex-col md:flex-row gap-4 justify-between md:items-center">
                                    <div>
                                        <p className="text-xs text-purple-400 font-bold tracking-wider mb-1">
                                            #{sale.orderNumber}
                                        </p>
                                        <p className="font-bold text-white mb-0.5">{sale.movieTitle}</p>
                                        <div className="flex items-center gap-2 text-[10px] text-gray-400 uppercase">
                                            <span className="flex items-center gap-1"><Clock className="w-3 h-3" /> {new Date(sale.bookingTime).toLocaleString('uk-UA')}</span>
                                            <span>•</span>
                                            <span>Місць: {sale.seats?.join(', ') || 'N/A'}</span>
                                        </div>
                                    </div>

                                    <div className="flex items-center justify-between md:justify-end gap-6">
                                        <div className="flex flex-col items-start md:items-end">
                                            <span className="text-lg font-black text-[#ffbd14]">{sale.totalPrice.toFixed(2)} ₴</span>
                                            <div className="flex items-center gap-1.5 text-[10px] font-bold text-gray-300 uppercase mt-0.5">
                                                {sale.paymentMethod.includes('Готівка') ? (
                                                    <><Banknote className="w-3 h-3 text-emerald-400" /> {sale.paymentMethod}</>
                                                ) : (
                                                    <><CreditCard className="w-3 h-3 text-blue-400" /> {sale.paymentMethod}</>
                                                )}
                                            </div>
                                        </div>
                                        <div className="px-3 py-1.5 rounded-lg text-[10px] font-black uppercase tracking-widest bg-emerald-500/10 text-emerald-400 border border-emerald-500/20">
                                            {sale.status}
                                        </div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            </div>
        </div>
    );
};
