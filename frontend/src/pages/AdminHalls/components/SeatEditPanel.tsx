import React from 'react';
import { Armchair } from 'lucide-react';
import type { AdminSeatDto } from '@/types';
import { SeatTypeMap, SeatStatusMap } from '@/types';

interface SeatEditPanelProps {
    selectedSeat: AdminSeatDto | null;
    onUpdateSeatProperties: (
        seatId: string,
        updatedTypeStr: 'Standard' | 'VIP' | 'Disabled',
        updatedStatusStr: 'Active' | 'Inactive'
    ) => void;
}

export const SeatEditPanel: React.FC<SeatEditPanelProps> = ({
    selectedSeat,
    onUpdateSeatProperties
}) => {
    return (
        <div className="lg:col-span-1 bg-[#111219] border border-white/5 p-5 rounded-2xl flex flex-col gap-5 sticky top-6">
            {selectedSeat ? (
                <>
                    <div>
                        <span className="text-[10px] text-[#ffbd14] font-black uppercase tracking-wider block">Обране місце</span>
                        <h4 className="text-xl font-black text-white mt-0.5">Ряд {selectedSeat.row}, Крісло {selectedSeat.number}</h4>
                    </div>

                    <div className="space-y-2">
                        <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider">Категорія (Тип)</label>
                        <div className="grid grid-cols-1 gap-2">
                            {(['Standard', 'VIP', 'Disabled'] as const).map(t => (
                                <button
                                    key={t}
                                    type="button"
                                    onClick={() => onUpdateSeatProperties(selectedSeat.id, t, SeatStatusMap[selectedSeat.status])}
                                    className={`w-full py-2.5 px-4 text-left text-xs font-bold rounded-xl border transition-all cursor-pointer ${SeatTypeMap[selectedSeat.type] === t
                                        ? 'bg-purple-600 border-purple-500 text-white shadow-lg'
                                        : 'bg-[#1a1c26] border-white/5 text-gray-400 hover:text-white hover:border-white/10'
                                        }`}
                                >
                                    {t === 'Standard' && 'Standard (Звичайне)'}
                                    {t === 'VIP' && 'VIP (Комфорт)'}
                                    {t === 'Disabled' && 'Disabled (Інклюзивне)'}
                                </button>
                            ))}
                        </div>
                    </div>

                    <div className="space-y-2 border-t border-white/5 pt-4">
                        <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider">Технічний Стан</label>
                        <div className="grid grid-cols-2 gap-2">
                            {(['Active', 'Inactive'] as const).map(s => (
                                <button
                                    key={s}
                                    type="button"
                                    onClick={() => onUpdateSeatProperties(selectedSeat.id, SeatTypeMap[selectedSeat.type], s)}
                                    className={`py-2 px-3 text-center text-xs font-black rounded-xl border transition-all uppercase tracking-wider cursor-pointer ${SeatStatusMap[selectedSeat.status] === s
                                        ? s === 'Active'
                                            ? 'bg-emerald-600 border-emerald-500 text-white'
                                            : 'bg-red-600 border-red-500 text-white'
                                        : 'bg-[#1a1c26] border-white/5 text-gray-400 hover:text-white'
                                        }`}
                                >
                                    {s}
                                </button>
                            ))}
                        </div>
                    </div>
                </>
            ) : (
                <div className="text-center py-12 border border-dashed border-white/5 rounded-xl">
                    <Armchair className="w-8 h-8 text-gray-600 mx-auto mb-2 opacity-50" />
                    <p className="text-xs text-gray-400 italic px-4">Клацніть на будь-яке крісло ліворуч для налаштування</p>
                </div>
            )}
        </div>
    );
};
