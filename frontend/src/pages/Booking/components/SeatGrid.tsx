import React from 'react';
import { Ban } from 'lucide-react';
import type { SeatMapDto } from '../types';

interface SeatGridProps {
    seats: SeatMapDto[];
    selectedSeats: SeatMapDto[];
    currentUserId: string | null;
    isCashierMode: boolean;
    onSeatClick: (seat: SeatMapDto) => void;
    onOpenHistory: () => void;
    isSalesLoading: boolean;
}

export const SeatGrid: React.FC<SeatGridProps> = ({
    seats,
    selectedSeats,
    currentUserId,
    isCashierMode,
    onSeatClick,
    onOpenHistory,
    isSalesLoading
}) => {
    const rows = Array.from(new Set(seats.map((s) => s.row)));

    return (
        <div className="w-full lg:col-span-3 bg-[#1a1c26] border border-white/5 p-6 sm:p-8 rounded-3xl shadow-2xl flex flex-col items-center overflow-x-auto text-xs">
            
            {isCashierMode && (
                <div className="w-full mb-6 flex items-center justify-between bg-purple-600/10 border border-purple-500/20 text-purple-400 px-4 py-2.5 rounded-xl font-bold uppercase tracking-wider">
                    <span className="flex items-center gap-2">🎟️ Термінал касира кінотеатру (Швидкий POS-друк)</span>
                    <button
                        type="button"
                        onClick={onOpenHistory}
                        className="flex items-center gap-1.5 bg-purple-600 hover:bg-purple-500 text-white px-3 py-1.5 rounded-lg text-[10px] uppercase font-black tracking-widest cursor-pointer transition-all border-none"
                    >
                        Журнал за зміну
                    </button>
                </div>
            )}

            <div className="w-full max-w-xl bg-gradient-to-b from-[#ffbd14]/20 to-transparent h-4 rounded-t-full mb-16 relative flex items-center justify-center border-t border-[#ffbd14]/30">
                <span className="text-[10px] text-[#ffbd14]/60 font-black tracking-[0.3em] uppercase absolute -bottom-6">Екран зали</span>
            </div>

            <div className="flex flex-col gap-3 w-full items-center min-w-[600px]">
                {rows.map((rowName) => (
                    <div key={rowName} className="flex items-center gap-4 w-full justify-center">
                        <span className="w-6 text-xs text-gray-500 font-bold text-center">{rowName}</span>
                        <div className="flex items-center gap-2">
                            {seats
                                .filter((s) => s.row === rowName)
                                .map((seat) => {
                                    const isSelected = selectedSeats.some((s) => s.seatId === seat.seatId);
                                    const isLockedBySomeoneElse = seat.status === 'Locked' && (!currentUserId || seat.lockedByUserId?.toLowerCase() !== currentUserId.toLowerCase());

                                    let seatClass = "bg-white/10 border-white/10 hover:border-[#ffbd14] hover:bg-white/20 text-white cursor-pointer";
                                    let content: React.ReactNode = seat.seatNumber;

                                    if (seat.status === 'Inactive') {
                                        seatClass = "bg-red-500/10 border-red-500/30 text-red-400 cursor-not-allowed";
                                        content = <Ban className="w-3 h-3" />;
                                    } else if (seat.status === 'Taken') {
                                        seatClass = "bg-red-500/20 border-red-500/10 text-red-500/40 cursor-not-allowed";
                                    } else if (isLockedBySomeoneElse) {
                                        seatClass = "bg-gray-600 border-gray-700 text-gray-400 cursor-not-allowed";
                                    } else if (isSelected) {
                                        seatClass = "bg-[#ffbd14] border-[#ffbd14] text-black font-black shadow-lg shadow-[#ffbd14]/20";
                                    } else if (seat.seatType === 'VIP') {
                                        seatClass = "bg-purple-600/20 border-purple-500/30 text-purple-400 hover:bg-purple-600/40";
                                    } else if (seat.seatType === 'Disabled') {
                                        seatClass = "bg-blue-600/20 border-blue-500/30 text-blue-400 hover:bg-blue-600/40";
                                    }

                                    return (
                                        <button
                                            key={seat.seatId}
                                            type="button"
                                            disabled={seat.status === 'Taken' || seat.status === 'Inactive' || isLockedBySomeoneElse}
                                            onClick={() => onSeatClick(seat)}
                                            className={`w-8 h-8 rounded-lg border text-[10px] font-bold flex items-center justify-center transition-all ${seatClass}`}
                                            title={seat.status === 'Inactive' ? "Місце зламане" : `Місце ${seat.seatNumber}`}
                                        >
                                            {content}
                                        </button>
                                    );
                                })}
                        </div>
                        <span className="w-6 text-xs text-gray-500 font-bold text-center">{rowName}</span>
                    </div>
                ))}
            </div>

            <div className="flex flex-wrap justify-center gap-6 mt-12 border-t border-white/5 pt-6 w-full text-xs text-gray-400">
                <div className="flex items-center gap-2">
                    <div className="w-4 h-4 bg-white/10 border border-white/10 rounded"></div>
                    <span>Вільне</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-4 h-4 bg-purple-600/20 border border-purple-500/30 rounded"></div>
                    <span>VIP (+50%)</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-4 h-4 bg-blue-600/20 border border-blue-500/30 rounded"></div>
                    <span>Інклюзивне (-20%)</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-4 h-4 bg-[#ffbd14] rounded"></div>
                    <span>Ваш вибір</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-4 h-4 bg-red-500/20 border border-red-500/10 rounded"></div>
                    <span>Зайняте</span>
                </div>
                <div className="flex items-center gap-2 text-red-400">
                    <div className="w-4 h-4 bg-red-500/10 border border-red-500/30 rounded flex items-center justify-center text-[8px]"><Ban className="w-2.5 h-2.5" /></div>
                    <span>Несправне</span>
                </div>
            </div>
        </div>
    );
};