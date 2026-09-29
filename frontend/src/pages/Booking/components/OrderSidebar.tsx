import React from 'react';
import { CreditCard, Banknote, Percent, CheckCircle } from 'lucide-react';
import type { SeatMapDto } from '@/types/bookings';

interface OrderSidebarProps {
    selectedSeats: SeatMapDto[];
    basePrice: number;
    isCashierMode: boolean;
    promoCode: string;
    setPromoCode: (code: string) => void;
    promoError: string | null;
    promoSuccess: string | null;
    isValidatingPromo: boolean;
    discountPercentage: number;
    cashierPaymentMethod: 'Cash' | 'Card';
    setCashierPaymentMethod: (method: 'Cash' | 'Card') => void;
    isSubmitting: boolean;
    lastBookingId: string | null;
    onValidatePromo: () => void;
    onCreateBooking: () => void;
    onProcessCashierSale: () => void;
    onTriggerPrint: (id: string) => void;
}

export const OrderSidebar: React.FC<OrderSidebarProps> = ({
    selectedSeats,
    basePrice,
    isCashierMode,
    promoCode,
    setPromoCode,
    promoError,
    promoSuccess,
    isValidatingPromo,
    discountPercentage,
    cashierPaymentMethod,
    setCashierPaymentMethod,
    isSubmitting,
    lastBookingId,
    onValidatePromo,
    onCreateBooking,
    onProcessCashierSale,
    onTriggerPrint
}) => {
    const calculateTotalPrice = () => {
        const baseTotal = selectedSeats.reduce((sum, seat) => sum + basePrice * (seat.priceMultiplier ?? 1), 0);
        if (discountPercentage > 0) {
            const m = baseTotal * (discountPercentage / 100) * 100;
            const integerPart = Math.floor(m);
            const fraction = m - integerPart;
            let discountAmount;

            if (Math.abs(fraction - 0.5) < 0.00001) {
                discountAmount = (integerPart % 2 === 0 ? integerPart : integerPart + 1) / 100;
            } else {
                discountAmount = Math.round(m) / 100;
            }

            return Number((baseTotal - discountAmount).toFixed(2));
        }
        return baseTotal;
    };

    return (
        <div className="lg:col-span-1 bg-[#1a1c26] border border-white/5 p-6 rounded-3xl shadow-2xl flex flex-col gap-6 text-xs min-w-0">
            <div>
                <h2 className="text-xl font-black tracking-tight text-white">Ваше замовлення</h2>
                <p className="text-gray-400 text-xs mt-1">Специфікація обраних квитків StarPlex</p>
            </div>

            <div className="flex flex-col gap-3 max-h-48 overflow-y-auto pr-1">
                {selectedSeats.length === 0 ? (
                    <p className="text-sm text-gray-500 italic py-4 text-center">Місця ще не обрано</p>
                ) : (
                    selectedSeats.map((seat) => (
                        <div key={seat.seatId} className="flex items-center justify-between bg-[#111219]/50 border border-white/5 p-3 rounded-xl text-xs font-semibold">
                            <div>
                                <p className="text-white">Ряд {seat.row}, Місце {seat.seatNumber}</p>
                                <p className="text-[10px] text-gray-500 uppercase tracking-wider mt-0.5">{seat.seatType} крісло</p>
                            </div>
                            <p className="font-bold text-[#ffbd14]">{basePrice * (seat.priceMultiplier ?? 1)} ₴</p>
                        </div>
                    ))
                )}
            </div>

            {selectedSeats.length > 0 && !isCashierMode && (
                <div className="bg-[#111219]/40 border border-white/5 p-4 rounded-2xl">
                    <label className="block text-[10px] font-bold text-gray-400 mb-2 uppercase tracking-wider flex items-center gap-1.5">
                        <Percent className="w-3.5 h-3.5 text-[#ffbd14]" /> Промокод на знижку
                    </label>
                    <div className="flex items-center gap-2 w-full">
                        <input
                            type="text"
                            placeholder="НАПР. STAR20"
                            value={promoCode}
                            disabled={!!promoSuccess}
                            onChange={(e) => setPromoCode(e.target.value.toUpperCase())}
                            className="flex-grow bg-dark-bg border border-white/10 rounded-xl px-3 py-2.5 text-xs text-white uppercase placeholder-gray-600 focus:outline-none focus:border-[#ffbd14] font-bold tracking-wider"
                        />
                        <button
                            type="button"
                            onClick={onValidatePromo}
                            disabled={isValidatingPromo || !promoCode || !!promoSuccess}
                            className="bg-[#ffbd14]/10 border border-[#ffbd14]/20 hover:bg-[#ffbd14] hover:text-black disabled:bg-white/5 text-[#ffbd14] px-4 py-2.5 rounded-xl font-bold transition-all cursor-pointer disabled:cursor-not-allowed text-xs"
                        >
                            {isValidatingPromo ? <div className="w-3.5 h-3.5 border-2 border-current border-t-transparent rounded-full animate-spin"></div> : 'Застосувати'}
                        </button>
                    </div>
                    {promoError && <p className="text-[11px] text-red-400 font-medium mt-2">⚠️ {promoError}</p>}
                    {promoSuccess && <p className="text-[11px] text-emerald-400 font-bold mt-2 flex items-center gap-1"><CheckCircle className="w-3.5 h-3.5" /> {promoSuccess}</p>}
                </div>
            )}

            <div className="border-t border-white/5 pt-4 flex flex-col gap-2">
                <div className="flex justify-between text-gray-400 font-semibold">
                    <span>Кількість квитків:</span>
                    <span className="font-bold text-white">{selectedSeats.length}</span>
                </div>
                <div className="flex justify-between items-end pt-2">
                    <span className="font-bold text-gray-300">Всього до сплати:</span>
                    <span className="text-2xl font-black text-[#ffbd14]">{calculateTotalPrice()} ₴</span>
                </div>
            </div>

            {isCashierMode && selectedSeats.length > 0 && (
                <div className="flex flex-col gap-3 border-t border-white/5 pt-4">
                    <span className="text-[10px] text-purple-400 font-bold uppercase tracking-wider">Касовий метод оплати</span>
                    <div className="grid grid-cols-2 gap-2">
                        <button type="button" onClick={() => setCashierPaymentMethod('Cash')} className={`py-2 px-3 rounded-xl border text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer ${cashierPaymentMethod === 'Cash' ? 'bg-purple-600 border-purple-500 text-white font-black' : 'bg-dark-bg border-white/5 text-gray-400'}`}>
                            <Banknote className="w-3.5 h-3.5" /> Готівка
                        </button>
                        <button type="button" onClick={() => setCashierPaymentMethod('Card')} className={`py-2 px-3 rounded-xl border text-xs font-bold transition-all flex items-center justify-center gap-1.5 cursor-pointer ${cashierPaymentMethod === 'Card' ? 'bg-purple-600 border-purple-500 text-white font-black' : 'bg-dark-bg border-white/5 text-gray-400'}`}>
                            <CreditCard className="w-3.5 h-3.5" /> Термінал
                        </button>
                    </div>
                    <button type="button" disabled={isSubmitting} onClick={onProcessCashierSale} className="w-full bg-purple-600 hover:bg-purple-500 text-white font-black py-3 px-4 rounded-xl shadow-lg transition-all uppercase tracking-wider mt-1 cursor-pointer border-none">
                        {isSubmitting ? 'Проведення оплати...' : '💵 Продати на касі'}
                    </button>
                </div>
            )}

            {isCashierMode && lastBookingId && (
                <div className="mt-2 bg-purple-600/10 border border-purple-500/20 p-3 rounded-2xl flex flex-col gap-2 animate-fadeIn">
                    <button type="button" onClick={() => onTriggerPrint(lastBookingId)} className="w-full bg-dark-bg border border-purple-500 hover:bg-purple-600 text-purple-400 hover:text-white text-xs font-black py-2.5 px-4 rounded-xl uppercase tracking-wider transition-all cursor-pointer flex items-center justify-center gap-2 border-none">
                        🔄 Повторити друк чеку
                    </button>
                </div>
            )}

            {(!isCashierMode || selectedSeats.length === 0) && (
                <button type="button" disabled={selectedSeats.length === 0 || isSubmitting} onClick={onCreateBooking} className="w-full bg-[#ffbd14] hover:bg-[#e0a410] disabled:bg-white/5 text-black disabled:text-gray-500 font-black py-3 px-4 rounded-xl shadow-lg transition-all text-sm disabled:cursor-not-allowed border-none cursor-pointer">
                    {isSubmitting ? 'Формування транзакції...' : 'Забронювати місця'}
                </button>
            )}
        </div>
    );
};
