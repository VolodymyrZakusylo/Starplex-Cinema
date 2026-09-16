import React, { useRef } from 'react';
import { Clock, Award, Plus, Edit2, Trash2 } from 'lucide-react';
import type { SessionDto, HallDto } from '@/types';

interface InteractiveTimelineProps {
    halls: HallDto[];
    filteredSessions: SessionDto[];
    selectedDate: string;
    onEditSession: (session: SessionDto) => void;
    onDeleteSession: (id: string) => void;
    onCreateSessionForSlot: (hallId: string, timeString: string) => void;
}

const START_HOUR = 10;
const END_HOUR = 23;
const HOUR_WIDTH = 110;
const MINUTE_WIDTH = HOUR_WIDTH / 60;
const CLEAN_UP_DURATION = 20;

export const InteractiveTimeline: React.FC<InteractiveTimelineProps> = ({
    halls,
    filteredSessions,
    selectedDate,
    onEditSession,
    onDeleteSession,
    onCreateSessionForSlot
}) => {
    const tracksRef = useRef<Record<string, HTMLDivElement | null>>({});

    const getMinutesFromStart = (isoString: string) => {
        if (!isoString) return 0;
        const date = new Date(isoString);
        const formatter = new Intl.DateTimeFormat('en-US', {
            timeZone: 'Europe/Kyiv',
            hour: 'numeric',
            minute: 'numeric',
            hour12: false
        });
        const parts = formatter.formatToParts(date);
        const hourVal = parts.find(p => p.type === 'hour')?.value || '0';
        const hour = parseInt(hourVal, 10) % 24;
        const minute = parseInt(parts.find(p => p.type === 'minute')?.value || '0', 10);
        return Math.max(0, (hour * 60 + minute) - START_HOUR * 60);
    };

    const formatKyivTime = (isoString: string) => {
        if (!isoString) return '00:00';
        return new Date(isoString).toLocaleTimeString('uk-UA', {
            hour: '2-digit',
            minute: '2-digit',
            timeZone: 'Europe/Kyiv'
        });
    };

    const handleTrackClick = (e: React.MouseEvent<HTMLDivElement>, hallId: string) => {
        if ((e.target as HTMLElement).closest('.session-block')) return;

        const trackEl = tracksRef.current[hallId];
        if (!trackEl) return;

        const rect = trackEl.getBoundingClientRect();
        const offsetX = e.clientX - rect.left;
        const clickedMinutesFromStart = Math.max(0, Math.round(offsetX / MINUTE_WIDTH));
        const snappedMinutesFromStart = Math.round(clickedMinutesFromStart / 5) * 5;
        const rawTotalMinutes = START_HOUR * 60 + snappedMinutesFromStart;
        const maxTotalMinutes = END_HOUR * 60 - 5; // 22:55 is the last valid start slot inside 10:00-23:00
        const minTotalMinutes = START_HOUR * 60;

        const totalMinutes = Math.max(minTotalMinutes, Math.min(maxTotalMinutes, rawTotalMinutes));
        const hour = Math.floor(totalMinutes / 60);
        const minute = totalMinutes % 60;

        const timeString = `${selectedDate}T${String(hour).padStart(2, '0')}:${String(minute).padStart(2, '0')}`;
        onCreateSessionForSlot(hallId, timeString);
    };

    const hourTicks = Array.from({ length: END_HOUR - START_HOUR + 1 }, (_, i) => START_HOUR + i);
    const totalTimelineWidth = hourTicks.length * HOUR_WIDTH;

    return (
        <div className="w-full bg-[#1a1c26] border border-white/5 p-6 rounded-2xl shadow-xl select-none flex flex-col gap-6 overflow-hidden text-xs">
            <div className="flex items-center justify-between border-b border-white/5 pb-4">
                <div className="flex items-center gap-2">
                    <Clock className="w-5 h-5 text-[#ffbd14]" />
                    <h2 className="text-lg font-black tracking-tight text-white">Візуальна таймлайн-сітка</h2>
                </div>
                <div className="flex items-center gap-4 text-[11px] text-gray-400">
                    <span className="flex items-center gap-1.5">
                        <span className="w-3 h-3 rounded-md bg-[#ffbd14] inline-block"></span> Фільм
                    </span>
                    <span className="flex items-center gap-1.5">
                        <span className="w-3 h-3 rounded-md bg-white/10 border border-dashed border-white/20 inline-block"></span> Клінінг (20 хв)
                    </span>
                    <span className="text-gray-500 italic">💡 Клікніть по вільному слоту для створення сеансу</span>
                </div>
            </div>

            <div className="w-full overflow-x-auto pb-4 no-scrollbar">
                <div style={{ width: `${totalTimelineWidth + 160}px` }} className="relative flex flex-col gap-4">
                    <div className="grid grid-cols-[160px_1fr] items-center w-full h-8 border-b border-white/5">
                        <div className="text-gray-500 font-mono text-[10px] pl-2 uppercase tracking-wider font-bold">
                            Зали / Час (Kyiv)
                        </div>
                        <div className="relative w-full h-full">
                            {hourTicks.map((hour, idx) => (
                                <div
                                    key={hour}
                                    className="absolute -translate-x-1/2 flex flex-col items-center text-gray-500 font-mono text-[10px]"
                                    style={{ left: `${idx * HOUR_WIDTH}px` }}
                                >
                                    <span>{String(hour).padStart(2, '0')}:00</span>
                                    <div className="w-px h-1.5 bg-white/10 mt-1"></div>
                                </div>
                            ))}
                        </div>
                    </div>

                    <div className="space-y-4 w-full">
                        {halls.map((hall) => {
                            const hallSessions = filteredSessions.filter(s => (s.hallId || (s as any).HallId) === hall.id);

                            return (
                                <div key={hall.id} className="grid grid-cols-[160px_1fr] items-center w-full h-[76px]">
                                    <div className="w-[150px] flex-shrink-0 font-black text-xs text-gray-300 truncate flex items-center justify-between sticky left-0 bg-[#1a1c26] z-30 py-2 pr-2">
                                        <span className="flex items-center gap-2 truncate">
                                            <Award className="w-3.5 h-3.5 text-[#ffbd14] shrink-0" />
                                            {hall.name.replace(" зал", "")}
                                        </span>
                                        <button
                                            type="button"
                                            onClick={() => onCreateSessionForSlot(hall.id, `${selectedDate}T12:00`)}
                                            className="p-1 hover:bg-white/10 rounded-md text-gray-400 hover:text-[#ffbd14] border-none bg-transparent cursor-pointer transition-colors"
                                            title="Додати сеанс у цей зал"
                                        >
                                            <Plus className="w-3.5 h-3.5" />
                                        </button>
                                    </div>

                                    <div
                                        ref={(el) => { tracksRef.current[hall.id] = el; }}
                                        onClick={(e) => handleTrackClick(e, hall.id)}
                                        style={{ width: `${(hourTicks.length - 1) * HOUR_WIDTH}px` }}
                                        className="h-full bg-[#111219]/60 border border-white/5 rounded-xl relative overflow-hidden backdrop-blur-sm transition-colors hover:bg-[#111219]/90 shadow-inner cursor-pointer group/track"
                                    >
                                        {hourTicks.map((_, idx) => (
                                            <div
                                                key={idx}
                                                className="absolute top-0 bottom-0 w-px bg-white/[0.04] pointer-events-none"
                                                style={{ left: `${idx * HOUR_WIDTH}px` }}
                                            />
                                        ))}

                                        {hallSessions.map((session) => {
                                            const sStartTime = session.startTime || (session as any).StartTime;
                                            const sEndTime = session.endTime || (session as any).EndTime;
                                            const movieTitle = session.movieTitle || (session as any).MovieTitle;
                                            const basePriceVal = session.basePrice || (session as any).BasePrice;
                                            const rawDuration = session.movieDurationInMinutes || (session as any).MovieDurationInMinutes;

                                            const duration = rawDuration || (sStartTime && sEndTime
                                                ? Math.max(1, Math.round((new Date(sEndTime).getTime() - new Date(sStartTime).getTime()) / 60000))
                                                : 120);

                                            const startMinutes = getMinutesFromStart(sStartTime);
                                            const leftPx = startMinutes * MINUTE_WIDTH;
                                            const movieWidthPx = duration * MINUTE_WIDTH;
                                            const cleanUpWidthPx = CLEAN_UP_DURATION * MINUTE_WIDTH;

                                            return (
                                                <div
                                                    key={session.id}
                                                    style={{ left: `${leftPx}px` }}
                                                    className="session-block absolute top-1 bottom-1 flex items-stretch z-10 hover:z-20 group"
                                                >
                                                    <div
                                                        onClick={(e) => { e.stopPropagation(); onEditSession(session); }}
                                                        style={{ width: `${movieWidthPx}px` }}
                                                        className="bg-[#ffbd14] text-black rounded-l-xl rounded-r-md p-2 flex flex-col justify-between shadow-md border border-black/10 cursor-pointer hover:brightness-105 transition-all overflow-hidden relative"
                                                        title={`${movieTitle} (${duration} хв)`}
                                                    >
                                                        <div className="flex justify-between items-start w-full relative min-w-0">
                                                            <p className="text-[11px] font-black leading-tight text-black line-clamp-2 pr-5 break-words w-full">
                                                                {movieTitle}
                                                            </p>
                                                            <div className="opacity-0 group-hover:opacity-100 flex gap-0.5 absolute right-0 top-0 transition-opacity bg-[#ffbd14] pl-1 rounded-bl-md z-30">
                                                                <button
                                                                    type="button"
                                                                    onClick={(e) => { e.stopPropagation(); onEditSession(session); }}
                                                                    className="p-0.5 hover:bg-black/15 rounded text-black border-none bg-transparent cursor-pointer"
                                                                    title="Редагувати"
                                                                >
                                                                    <Edit2 className="w-3 h-3 stroke-[2.5]" />
                                                                </button>
                                                                <button
                                                                    type="button"
                                                                    onClick={(e) => { e.stopPropagation(); onDeleteSession(session.id); }}
                                                                    className="p-0.5 hover:bg-black/15 rounded text-red-700 hover:text-red-900 border-none bg-transparent cursor-pointer"
                                                                    title="Видалити"
                                                                >
                                                                    <Trash2 className="w-3 h-3 stroke-[2.5]" />
                                                                </button>
                                                            </div>
                                                        </div>

                                                        <div className="flex justify-between items-center w-full border-t border-black/15 pt-1 mt-auto leading-none min-w-0">
                                                            <span className="text-[10px] font-black tracking-tight flex items-center gap-0.5 opacity-90 shrink-0">
                                                                <Clock className="w-2.5 h-2.5 stroke-[2.5]" />
                                                                {formatKyivTime(sStartTime)}
                                                            </span>
                                                            <span className="text-[10px] font-black bg-black/15 px-1 py-0.5 rounded shrink-0">
                                                                {basePriceVal}₴
                                                            </span>
                                                        </div>
                                                    </div>

                                                    <div
                                                        style={{ width: `${cleanUpWidthPx}px` }}
                                                        className="bg-[#ffbd14]/20 border-y border-r border-[#ffbd14]/30 rounded-r-xl flex items-center justify-center opacity-60 group-hover:opacity-100 transition-opacity"
                                                        title="Клінінг залу (20 хв)"
                                                    >
                                                        <span className="text-[8px] font-mono text-white/50 select-none">🧹</span>
                                                    </div>
                                                </div>
                                            );
                                        })}
                                    </div>
                                </div>
                            );
                        })}
                    </div>
                </div>
            </div>
        </div>
    );
};