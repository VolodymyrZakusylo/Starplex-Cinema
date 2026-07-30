import React from 'react';
import { Trash2, Edit2, Calendar, LayoutGrid, Clock } from 'lucide-react';
import type { SessionDto, HallDto } from '@/types';

interface ManualScheduleManagerProps {
    halls: HallDto[];
    filteredSessions: SessionDto[];
    hallIdFromUrl: string | null;
    clearHallFilter: () => void;
    handleEditOpen: (session: SessionDto) => void;
    handleDelete: (id: string) => void;
    formatTime: (isoString: string) => string;
}

export const ManualScheduleManager: React.FC<ManualScheduleManagerProps> = ({
    halls,
    filteredSessions,
    hallIdFromUrl,
    clearHallFilter,
    handleEditOpen,
    handleDelete,
    formatTime
}) => {
    const displayHalls = hallIdFromUrl ? halls.filter(h => h.id === hallIdFromUrl) : halls;

    return (
        <div className="space-y-8 animate-fadeIn text-xs">
            {hallIdFromUrl && (
                <div className="flex items-center justify-between bg-[#ffbd14]/10 border border-[#ffbd14]/20 rounded-xl p-4">
                    <div className="flex items-center gap-2 text-sm text-gray-300">
                        <LayoutGrid className="w-4 h-4 text-[#ffbd14]" />
                        <span>Фільтр залу: <strong className="text-white">{halls.find(h => h.id === hallIdFromUrl)?.name || 'Обраний зал'}</strong></span>
                    </div>
                    <button onClick={clearHallFilter} className="flex items-center gap-1 text-xs font-black text-gray-400 hover:text-[#ffbd14] uppercase tracking-wider transition-colors border-none bg-transparent cursor-pointer">
                        Скинути фільтр
                    </button>
                </div>
            )}

            {displayHalls.map((hall) => {
                const hallSessions = filteredSessions.filter(s => (s.hallId || (s as any).HallId) === hall.id)
                    .sort((a, b) => (a.startTime || '').localeCompare(b.startTime || ''));

                return (
                    <div key={hall.id} className="bg-[#1a1c26] border border-white/5 rounded-2xl p-6 shadow-md">
                        <h2 className="text-lg font-black tracking-tight text-white mb-4 flex items-center gap-2 border-b border-white/5 pb-3">
                            <LayoutGrid className="w-4 h-4 text-gray-500" /> {hall.name}
                        </h2>

                        {hallSessions.length === 0 ? (
                            <p className="text-gray-500 italic py-2">На цю дату сеансів немає.</p>
                        ) : (
                            <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-4">
                                {hallSessions.map((session) => {
                                    const movieTitle = session.movieTitle || (session as any).MovieTitle;
                                    const basePriceVal = session.basePrice || (session as any).BasePrice;
                                    const sStartTime = session.startTime || (session as any).StartTime;
                                    const sEndTime = session.endTime || (session as any).EndTime;

                                    const duration = sStartTime && sEndTime
                                        ? Math.round((new Date(sEndTime).getTime() - new Date(sStartTime).getTime()) / 60000)
                                        : 0;

                                    return (
                                        <div key={session.id} className="bg-[#111219] border border-white/5 rounded-xl p-4 relative group hover:border-[#ffbd14]/30 transition-all shadow-md">
                                            <div className="flex justify-between items-start mb-2">
                                                <span className="text-xl font-black tracking-tight text-[#ffbd14]">{formatTime(sStartTime)}</span>
                                                <div className="opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1.5">
                                                    <button onClick={() => handleEditOpen(session)} className="p-1.5 bg-white/5 hover:bg-white/10 rounded-md text-gray-400 hover:text-white border-none cursor-pointer"><Edit2 className="w-3.5 h-3.5" /></button>
                                                    <button onClick={() => handleDelete(session.id)} className="p-1.5 bg-red-500/10 hover:bg-red-500/20 rounded-md text-red-400 border-none cursor-pointer"><Trash2 className="w-3.5 h-3.5" /></button>
                                                </div>
                                            </div>
                                            <h4 className="font-bold text-sm text-white line-clamp-1 mb-1">{movieTitle}</h4>
                                            <div className="flex items-center justify-between text-xs text-gray-400 mt-3 border-t border-white/5 pt-2">
                                                <span className="flex items-center gap-1"><Clock className="w-3 h-3" /> {duration} хв</span>
                                                <span className="font-bold text-emerald-400">{basePriceVal} ₴</span>
                                            </div>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </div>
                );
            })}
        </div>
    );
};