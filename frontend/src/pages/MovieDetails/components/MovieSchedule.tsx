import React from 'react';
import { Link } from 'react-router-dom';
import type { Session } from '@/types/index';

interface MovieScheduleProps {
    sessions: Session[];
    selectedCinemaId: string | null;
    activeDay: string;
    setActiveDay: (day: string) => void;
}

export const MovieSchedule: React.FC<MovieScheduleProps> = ({ sessions, selectedCinemaId, activeDay, setActiveDay }) => {
    const now = new Date();

    const allowedSessions = sessions?.filter(s =>
        s.cinemaId === selectedCinemaId &&
        s.status === 0 &&
        new Date(s.startTime) > now
    ) || [];

    const groupedByDate: Record<string, { tabLabel: string; sessions: Session[] }> = {};

    allowedSessions.forEach((session) => {
        const dateObj = new Date(session.startTime);
        const tabLabel = dateObj.toLocaleDateString('uk-UA', { weekday: 'short', day: 'numeric', month: 'short' });
        const fullLabel = dateObj.toLocaleDateString('uk-UA', { weekday: 'long', day: 'numeric', month: 'long' });
        const formattedFullLabel = fullLabel.charAt(0).toUpperCase() + fullLabel.slice(1);

        if (!groupedByDate[formattedFullLabel]) {
            groupedByDate[formattedFullLabel] = {
                tabLabel: tabLabel.charAt(0).toUpperCase() + tabLabel.slice(1),
                sessions: []
            };
        }
        groupedByDate[formattedFullLabel].sessions.push(session);
    });

    const dateEntries = Object.entries(groupedByDate).sort(([_, dataA], [__, dataB]) => {
        const timeA = new Date(dataA.sessions[0]?.startTime || 0).getTime();
        const timeB = new Date(dataB.sessions[0]?.startTime || 0).getTime();
        return timeA - timeB;
    });
    
    const currentActiveDay = activeDay || dateEntries[0]?.[0] || "";
    const activeData = groupedByDate[currentActiveDay];

    const activeSessionsSorted = activeData?.sessions
        ? [...activeData.sessions].sort((a, b) => new Date(a.startTime).getTime() - new Date(b.startTime).getTime())
        : [];

    if (dateEntries.length === 0) {
        return (
            <div className="bg-[#1a1c26] border border-white/5 p-6 rounded-2xl text-center shadow-xl lg:col-span-1">
                <p className="text-xs text-gray-500 italic">На жаль, у цьому місті на найближчі дні вільних сеансів не знайдено 🍿</p>
            </div>
        );
    }

    return (
        <div className="lg:col-span-1 text-xs">
            <h2 className="text-xl font-black mb-1 tracking-tight">Розклад сеансів</h2>
            <p className="text-gray-400 text-xs mb-6">Оберіть зручний час для переходу до бронювання місць.</p>

            <div className="flex gap-2 overflow-x-auto pb-3 mb-6 no-scrollbar">
                {dateEntries.map(([fullLabel, data]) => {
                    const isSelected = currentActiveDay === fullLabel;
                    return (
                        <button
                            key={fullLabel}
                            type="button"
                            onClick={() => setActiveDay(fullLabel)}
                            className={`flex flex-col items-center justify-center min-w-[85px] py-2.5 px-3 rounded-xl border text-center transition-all cursor-pointer ${
                                isSelected 
                                    ? "bg-[#ffbd14] border-[#ffbd14] text-black font-black shadow-lg shadow-[#ffbd14]/10" 
                                    : "bg-[#1a1c26] border-white/5 text-gray-400 hover:text-white bg-transparent"
                            }`}
                        >
                            <span className="text-[10px] uppercase tracking-wider font-bold">{data.tabLabel.split(',')[0]}</span>
                            <span className="text-sm font-black mt-0.5">{data.tabLabel.split(',')[1]}</span>
                        </button>
                    );
                })}
            </div>

            <div className="bg-[#1a1c26] border border-white/5 p-5 rounded-2xl shadow-xl">
                <h3 className="text-[10px] font-black text-[#ffbd14] uppercase tracking-widest mb-4 border-b border-white/5 pb-2">
                    📅 {currentActiveDay}
                </h3>

                {activeSessionsSorted.length === 0 ? (
                    <p className="text-gray-500 italic py-2">На сьогодні більше немає активних сеансів.</p>
                ) : (
                    <div className="grid grid-cols-3 gap-3">
                        {activeSessionsSorted.map((session) => {
                            const sessionTime = new Date(session.startTime).toLocaleTimeString('uk-UA', {
                                hour: '2-digit', minute: '2-digit', timeZone: 'UTC'
                            });
                            return (
                                <Link
                                    key={session.id}
                                    to={`/booking/${session.id}`}
                                    state={{ basePrice: session.basePrice }}
                                    className="flex flex-col items-center justify-center bg-[#111219] border border-white/5 hover:border-[#ffbd14] text-white hover:text-[#ffbd14] p-2.5 rounded-xl transition-all text-center group font-bold shadow-md"
                                >
                                    <span className="text-[9px] uppercase text-gray-500 group-hover:text-[#ffbd14]/80 mb-1 truncate w-full">
                                        {session.hallName || "Зал"}
                                    </span>
                                    <span className="text-sm font-black">{sessionTime}</span>
                                </Link>
                            );
                        })}
                    </div>
                )}
            </div>
        </div>
    );
};