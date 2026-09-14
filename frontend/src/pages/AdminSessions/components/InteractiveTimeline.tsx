import React, { useRef, useState } from 'react';
import { Clock, Award, Trash2, Edit2 } from 'lucide-react';
import { sessionsApi } from '@/api/sessions';
import { useToast } from '@/hooks/useToast';
import type { SessionDto, HallDto } from '@/types';

interface InteractiveTimelineProps {
    halls: HallDto[];
    filteredSessions: SessionDto[];
    selectedDate: string;
    onScheduleUpdated: () => void;
    onDeleteSession: (id: string) => void;
    onEditSession: (session: SessionDto) => void;
}

const START_HOUR = 10;
const END_HOUR = 23;
const HOUR_WIDTH = 120;
const MINUTE_WIDTH = HOUR_WIDTH / 60;
const CLEAN_UP_DURATION = 20;

export const InteractiveTimeline: React.FC<InteractiveTimelineProps> = ({
    halls,
    filteredSessions,
    selectedDate,
    onScheduleUpdated,
    onDeleteSession,
    onEditSession
}) => {
    const { showError } = useToast();
    const timelinesRef = useRef<Record<string, HTMLDivElement | null>>({});
    const [isDraggingOverTrash, setIsDraggingOverTrash] = useState(false);

    const getMinutesFromStart = (isoString: string) => {
        const date = new Date(isoString);
        return Math.max(0, (date.getUTCHours() * 60 + date.getUTCMinutes()) - START_HOUR * 60);
    };

    const calculatePixelPosition = (startTime: string, movieDuration: number) => {
        const startMinutes = getMinutesFromStart(startTime);
        const totalDuration = movieDuration + CLEAN_UP_DURATION;

        const left = startMinutes * MINUTE_WIDTH;
        const width = totalDuration * MINUTE_WIDTH;

        return { left: `${left}px`, width: `${width}px` };
    };

    const handleDragStart = (e: React.DragEvent, sessionId: string) => {
        e.dataTransfer.setData('text/plain', sessionId);
    };

    const handleDragOver = (e: React.DragEvent) => {
        e.preventDefault();
    };

    const handleDrop = async (e: React.DragEvent, targetHallId: string) => {
        e.preventDefault();
        const sessionId = e.dataTransfer.getData('text/plain');
        const timelineTrack = timelinesRef.current[targetHallId];

        if (!timelineTrack || !sessionId) return;

        const rect = timelineTrack.getBoundingClientRect();
        const scrollLeft = timelineTrack.closest('.overflow-x-auto')?.scrollLeft || 0;
        const offsetX = e.clientX - rect.left + scrollLeft;

        const droppedMinutes = Math.round(offsetX / MINUTE_WIDTH);
        const targetTotalMinutes = START_HOUR * 60 + droppedMinutes;
        const roundedMinutes = Math.round(targetTotalMinutes / 5) * 5;

        const hours = Math.floor(roundedMinutes / 60);
        const minutes = roundedMinutes % 60;

        if (hours < START_HOUR || hours >= END_HOUR) {
            showError('Помилка переміщення: Сеанс виходить за межі робочого часу кінотеатру (10:00 - 23:00)');
            return;
        }

        const newStartTime = new Date(selectedDate);
        newStartTime.setUTCHours(hours, minutes, 0, 0);

        try {
            await sessionsApi.move(sessionId, newStartTime.toISOString());
            onScheduleUpdated();
        } catch (err: any) {
            showError(err.response?.data?.message || 'Обраний слот уже зайнятий іншим сеансом.');
        }
    };

    const handleTrashDrop = (e: React.DragEvent) => {
        e.preventDefault();
        setIsDraggingOverTrash(false);
        const sessionId = e.dataTransfer.getData('text/plain');
        if (sessionId) {
            onDeleteSession(sessionId);
        }
    };

    const hourTicks = Array.from({ length: END_HOUR - START_HOUR + 1 }, (_, i) => START_HOUR + i);
    const totalTimelineWidth = hourTicks.length * HOUR_WIDTH;

    return (
        <div className="w-full bg-[#1a1c26] border border-white/5 p-6 rounded-2xl shadow-xl select-none flex flex-col gap-6 overflow-hidden">
            <div className="w-full overflow-x-auto pb-4 no-scrollbar">
                <div style={{ width: `${totalTimelineWidth + 160}px` }} className="relative flex flex-col gap-4">
                    
                    <div className="grid grid-cols-[160px_1fr] items-center w-full h-8 border-b border-white/5">
                        <div className="text-gray-500 font-mono text-[10px] pl-2 uppercase tracking-wider font-bold">Зали / Час</div>
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
                                    <div className="w-[150px] flex-shrink-0 font-black text-xs text-gray-300 truncate flex items-center gap-2 sticky left-0 bg-[#1a1c26] z-30 py-2 pr-2">
                                        <Award className="w-3.5 h-3.5 text-[#ffbd14]" />
                                        {hall.name.replace(" зал", "")}
                                    </div>

                                    <div
                                        ref={(el) => { timelinesRef.current[hall.id] = el; }}
                                        onDragOver={handleDragOver}
                                        onDrop={(e) => handleDrop(e, hall.id)}
                                        style={{ width: `${(hourTicks.length - 1) * HOUR_WIDTH}px` }}
                                        className="h-full bg-[#111219]/60 border border-white/5 rounded-xl relative overflow-hidden backdrop-blur-sm transition-colors hover:bg-[#111219]/90 shadow-inner"
                                    >
                                        {hallSessions.map((session) => {
                                            const sStartTime = session.startTime || (session as any).StartTime;
                                            const movieTitle = session.movieTitle || (session as any).MovieTitle;
                                            const basePriceVal = session.basePrice || (session as any).BasePrice;
                                            const duration = session.movieDurationInMinutes || (session as any).MovieDurationInMinutes || 120;
                                            const { left, width } = calculatePixelPosition(sStartTime, duration);

                                            return (
                                                <div
                                                    key={session.id}
                                                    draggable
                                                    onDragStart={(e) => handleDragStart(e, session.id)}
                                                    style={{ left, width }}
                                                    className="absolute top-1 bottom-1 bg-[#ffbd14] text-black rounded-xl p-2.5 flex flex-col justify-between shadow-md border border-black/10 group cursor-grab active:cursor-grabbing hover:brightness-105 transition-all z-10 hover:z-20 overflow-hidden"
                                                    title={`${movieTitle} (${duration} хв)`}
                                                >
                                                    <div className="flex justify-between items-start w-full relative min-w-0">
                                                        <p className="text-[11px] font-black leading-tight text-black line-clamp-2 pr-6 break-words w-full">
                                                            {movieTitle}
                                                        </p>
                                                        <div className="opacity-0 group-hover:opacity-100 flex gap-0.5 absolute right-0 top-0 transition-opacity bg-[#ffbd14] pl-1 rounded-bl-md z-30">
                                                            <button type="button" onClick={(e) => { e.stopPropagation(); onEditSession(session); }} className="p-0.5 hover:bg-black/15 rounded-md text-black border-none bg-transparent cursor-pointer"><Edit2 className="w-3 h-3 stroke-[2.5]" /></button>
                                                            <button type="button" onClick={(e) => { e.stopPropagation(); onDeleteSession(session.id); }} className="p-0.5 hover:bg-black/15 rounded-md text-red-700 hover:text-red-900 border-none bg-transparent cursor-pointer"><Trash2 className="w-3 h-3 stroke-[2.5]" /></button>
                                                        </div>
                                                    </div>

                                                    <div className="flex justify-between items-center w-full border-t border-black/15 pt-1 mt-auto leading-none min-w-0">
                                                        <span className="text-[10px] font-black tracking-tight flex items-center gap-0.5 opacity-90 shrink-0">
                                                            <Clock className="w-2.5 h-2.5 stroke-[2.5]" />
                                                            {new Date(sStartTime).toLocaleTimeString('uk-UA', { hour: '2-digit', minute: '2-digit', timeZone: 'UTC' })}
                                                        </span>
                                                        <span className="text-[10px] font-black bg-black/15 px-1 py-0.5 rounded-md shrink-0">
                                                            {basePriceVal}₴
                                                        </span>
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

            <div
                onDragOver={(e) => { e.preventDefault(); setIsDraggingOverTrash(true); }}
                onDragLeave={() => setIsDraggingOverTrash(false)}
                onDrop={handleTrashDrop}
                className={`w-full border-2 border-dashed rounded-xl p-4 flex items-center justify-center gap-2 text-xs font-bold uppercase tracking-wider transition-all ${isDraggingOverTrash ? 'bg-red-500/10 border-red-500 text-red-500 scale-[0.99]' : 'bg-white/5 border-white/10 text-gray-500'}`}
            >
                <Trash2 className="w-4 h-4" />
                <span>Перетягніть сеанс сюди для скасування / видалення</span>
            </div>
        </div>
    );
};