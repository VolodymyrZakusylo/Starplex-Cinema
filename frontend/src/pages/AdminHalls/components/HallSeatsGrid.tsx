import React from 'react';
import { Ban } from 'lucide-react';
import type { AdminSeatDto } from '@/types';
import { SeatTypeMap } from '@/types';

interface HallSeatsGridProps {
    hallSeats: AdminSeatDto[];
    selectedSeatForEdit: AdminSeatDto | null;
    onSelectSeat: (seat: AdminSeatDto) => void;
}

export const HallSeatsGrid: React.FC<HallSeatsGridProps> = ({
    hallSeats,
    selectedSeatForEdit,
    onSelectSeat
}) => {
    const rowsMap: Record<string, AdminSeatDto[]> = {};
    hallSeats.forEach(seat => {
        if (!rowsMap[seat.row]) rowsMap[seat.row] = [];
        rowsMap[seat.row].push(seat);
    });

    const sortedRowEntries = Object.entries(rowsMap)
        .sort(([a], [b]) => a.localeCompare(b, undefined, { numeric: true }));

    return (
        <div className="flex flex-col items-center overflow-x-auto py-4 bg-[#111219]/40 border border-white/5 rounded-2xl p-6">
            <div className="w-full max-w-md bg-gradient-to-b from-[#ffbd14]/10 to-transparent h-3 rounded-t-full mb-12 relative flex items-center justify-center border-t border-[#ffbd14]/20">
                <span className="text-[9px] text-[#ffbd14]/40 font-bold tracking-[0.3em] uppercase absolute -bottom-5">Екран зали</span>
            </div>

            <div className="flex flex-col gap-2.5 min-w-[500px]">
                {sortedRowEntries.map(([rowName, seats]) => (
                    <div key={rowName} className="flex items-center gap-3 justify-center w-full">
                        <span className="w-5 text-[11px] font-black text-gray-500 text-center">{rowName}</span>
                        <div className="flex items-center gap-1.5">
                            {seats
                                .sort((a, b) => a.number - b.number)
                                .map(seat => {
                                    const isBeingEdited = selectedSeatForEdit?.id === seat.id;

                                    let seatClass = "bg-white/10 border-white/10 hover:border-[#ffbd14] text-white";
                                    if (seat.status === 1) seatClass = "bg-red-500/10 border-red-500/40 text-red-400 hover:bg-red-500/20";
                                    else if (seat.type === 1) seatClass = "bg-purple-600/20 border-purple-500/40 text-purple-400 hover:bg-purple-600/30";
                                    else if (seat.type === 2) seatClass = "bg-blue-600/20 border-blue-500/40 text-blue-400 hover:bg-blue-600/30";

                                    if (isBeingEdited) seatClass = "bg-[#ffbd14] border-[#ffbd14] text-black font-black ring-4 ring-[#ffbd14]/20 scale-105";

                                    return (
                                        <button
                                            key={seat.id}
                                            type="button"
                                            onClick={() => onSelectSeat(seat)}
                                            className={`w-7 h-7 rounded-md border text-[9px] font-black flex items-center justify-center transition-all cursor-pointer ${seatClass}`}
                                            title={`Ряд ${seat.row}, Місце ${seat.number} (${SeatTypeMap[seat.type]})`}
                                        >
                                            {seat.status === 1 ? <Ban className="w-2.5 h-2.5" /> : seat.number}
                                        </button>
                                    );
                                })}
                        </div>
                        <span className="w-5 text-[11px] font-black text-gray-500 text-center">{rowName}</span>
                    </div>
                ))}
            </div>

            <div className="flex flex-wrap justify-center gap-6 mt-10 border-t border-white/5 pt-5 w-full text-[11px] text-gray-400">
                <div className="flex items-center gap-2">
                    <div className="w-3 h-3 bg-white/10 border border-white/10 rounded"></div>
                    <span>Standard</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-3 h-3 bg-purple-600/20 border border-purple-500/30 rounded"></div>
                    <span>VIP (+50%)</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-3 h-3 bg-blue-600/20 border border-blue-500/30 rounded"></div>
                    <span>Disabled</span>
                </div>
                <div className="flex items-center gap-2">
                    <div className="w-3 h-3 bg-red-500/10 border border-red-500/30 flex items-center justify-center rounded text-red-400"><Ban className="w-2 h-2" /></div>
                    <span>Inactive (Зламане)</span>
                </div>
            </div>
        </div>
    );
};
