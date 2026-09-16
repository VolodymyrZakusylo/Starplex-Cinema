import React, { useEffect, useState } from 'react';
import { useAuthStore } from '@/store/authStore';
import { useSearchParams } from 'react-router-dom';
import { Plus, Clock, Sliders, ChevronDown, ChevronUp } from 'lucide-react';
import { moviesApi } from '@/api/movies';
import { cinemasApi } from '@/api/cinemas';
import { hallsApi } from '@/api/halls';
import { sessionsApi } from '@/api/sessions';
import { useToast } from '@/hooks/useToast';
import { getKyivDateString } from '@/utils/date';

import { ScheduleGenerationPanel } from './components/ScheduleGenerationPanel';
import { InteractiveTimeline } from './components/InteractiveTimeline';
import { SessionFormModal, type SessionFormPayload } from './components/SessionFormModal';

import type { SessionDto, MovieDto, HallDto, CinemaDto } from '@/types';

export const AdminSessionsPage: React.FC = () => {
    const { user } = useAuthStore();
    const isSuperAdmin = user?.roles.includes('SuperAdmin');
    const [searchParams, setSearchParams] = useSearchParams();

    const cinemaIdFromUrl = searchParams.get('cinemaId');
    const hallIdFromUrl = searchParams.get('hallId');

    const [selectedCinemaId, setSelectedCinemaId] = useState<string>('');
    const [cinemas, setCinemas] = useState<CinemaDto[]>([]);
    const [halls, setHalls] = useState<HallDto[]>([]);
    const [movies, setMovies] = useState<MovieDto[]>([]);
    const [sessions, setSessions] = useState<SessionDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [selectedDate, setSelectedDate] = useState<string>(() => getKyivDateString(0));
    const [dateTabs, setDateTabs] = useState<string[]>([]);

    const [showGeneratorPanel, setShowGeneratorPanel] = useState<boolean>(false);

    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
    const [editingSession, setEditingSession] = useState<SessionDto | null>(null);
    const [modalInitialHallId, setModalInitialHallId] = useState<string>('');
    const [modalInitialStartTime, setModalInitialStartTime] = useState<string>('');

    const { confirm, showError, showSuccess } = useToast();

    useEffect(() => {
        const dates = [];
        for (let i = 0; i < 7; i++) {
            dates.push(getKyivDateString(i));
        }
        setDateTabs(dates);
    }, []);

    useEffect(() => {
        const initializeData = async () => {
            try {
                const moviesData = await moviesApi.getAll();
                setMovies(moviesData);

                if (isSuperAdmin) {
                    const cinemasData = await cinemasApi.getAll();
                    setCinemas(cinemasData);

                    if (cinemasData.length > 0) {
                        if (cinemaIdFromUrl && cinemasData.some(c => c.id === cinemaIdFromUrl)) {
                            setSelectedCinemaId(cinemaIdFromUrl);
                        } else {
                            setSelectedCinemaId(cinemasData[0].id);
                        }
                    }
                } else if (user?.cinemaId) {
                    setSelectedCinemaId(user.cinemaId);
                }
            } catch (err: any) {
                showError('Помилка ініціалізації даних сховища.');
            }
        };
        initializeData();
    }, [isSuperAdmin, user, cinemaIdFromUrl]);

    const fetchHallsAndSessions = async () => {
        if (!selectedCinemaId) return;
        setIsLoading(true);
        try {
            const hallsData = await hallsApi.getByCinema(selectedCinemaId);
            const activeHalls = hallsData.filter(h => h.isActive);
            setHalls(activeHalls);

            const sessionsData = await sessionsApi.getByCinema(selectedCinemaId);
            setSessions(sessionsData);
        } catch (err) {
            showError('Не вдалося завантажити сеанси або зали.');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchHallsAndSessions();
    }, [selectedCinemaId]);

    const handleCinemaChange = (cinemaId: string) => {
        setSelectedCinemaId(cinemaId);
        setSearchParams({ cinemaId });
    };

    const displayHalls = hallIdFromUrl ? halls.filter(h => h.id === hallIdFromUrl) : halls;

    const filteredSessions = sessions.filter(s => {
        const timeRaw = s.startTime || (s as any).StartTime;
        const currentStatus = s.status ?? (s as any).Status;

        if (!timeRaw) return false;
        const isActive = (currentStatus as any) === 'Active' || (currentStatus as any) === 0;

        const sessionKyivDate = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Kyiv' }).format(new Date(timeRaw));
        return sessionKyivDate === selectedDate && isActive;
    });

    const handleCreateOpen = (hallId?: string, timeString?: string) => {
        if (halls.length === 0 || movies.length === 0) {
            showError('Для створення сеансу в базі повинні бути активні зали та фільми!');
            return;
        }
        setEditingSession(null);
        setModalInitialHallId(hallId || (hallIdFromUrl && halls.some(h => h.id === hallIdFromUrl) ? hallIdFromUrl : halls[0].id));
        setModalInitialStartTime(timeString || `${selectedDate}T12:00`);
        setIsModalOpen(true);
    };

    const handleEditOpen = (session: SessionDto) => {
        setEditingSession(session);
        setModalInitialHallId('');
        setModalInitialStartTime('');
        setIsModalOpen(true);
    };

    const handleSaveSession = async (payload: SessionFormPayload) => {
        const fullPayload = {
            ...payload,
            cinemaId: selectedCinemaId
        };

        if (editingSession) {
            await sessionsApi.update(editingSession.id, fullPayload);
            showSuccess('Сеанс успішно відредаговано.');
        } else {
            await sessionsApi.create(fullPayload);
            showSuccess('Новий сеанс успішно внесено до сітки залу.');
        }
        fetchHallsAndSessions();
    };

    const handleDeleteSession = async (id: string) => {
        confirm('Скасувати цей сеанс? Клієнтам автоматично повернуться кошти на картки.', async () => {
            try {
                await sessionsApi.delete(id);
                showSuccess('Сеанс скасовано, кошти відправлено на повернення.');
                setIsModalOpen(false);
                fetchHallsAndSessions();
            } catch (err: any) {
                showError(err.response?.data?.message || 'Не вдалося видалити сеанс.');
            }
        });
    };

    return (
        <div className="w-full select-none text-xs">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6 mb-6 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <Clock className="text-[#ffbd14] w-8 h-8" /> Управління розкладом
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">Таймлайн-сітка залів та розумна автоматична генерація</p>
                </div>

                <div className="flex flex-wrap items-center gap-3 w-full sm:w-auto">
                    {isSuperAdmin && cinemas.length > 0 && (
                        <select
                            value={selectedCinemaId}
                            onChange={(e) => handleCinemaChange(e.target.value)}
                            className="bg-[#1a1c26] border border-white/10 rounded-xl px-4 py-2.5 text-xs font-semibold text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer"
                        >
                            {cinemas.map(c => (
                                <option key={c.id} value={c.id}>{c.city} — {c.name}</option>
                            ))}
                        </select>
                    )}

                    <button
                        type="button"
                        onClick={() => setShowGeneratorPanel(!showGeneratorPanel)}
                        className={`px-4 py-2.5 rounded-xl font-bold transition-all flex items-center gap-2 border cursor-pointer ${
                            showGeneratorPanel
                                ? 'bg-[#ffbd14]/10 border-[#ffbd14]/40 text-[#ffbd14]'
                                : 'bg-[#1a1c26] border-white/10 text-gray-300 hover:text-white'
                        }`}
                    >
                        <Sliders className="w-4 h-4 text-[#ffbd14]" />
                        <span>⚡ Автогенерація</span>
                        {showGeneratorPanel ? <ChevronUp className="w-3.5 h-3.5" /> : <ChevronDown className="w-3.5 h-3.5" />}
                    </button>

                    <button
                        type="button"
                        onClick={() => handleCreateOpen()}
                        className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-black px-5 py-2.5 rounded-xl transition-all shadow-lg flex items-center gap-2 uppercase tracking-wider border-none cursor-pointer"
                    >
                        <Plus className="w-4 h-4 stroke-[3]" /> Додати сеанс
                    </button>
                </div>
            </div>

            {showGeneratorPanel && (
                <div className="mb-8 animate-fadeIn">
                    <ScheduleGenerationPanel
                        currentCinemaId={selectedCinemaId}
                        isSuperAdmin={isSuperAdmin || false}
                        cinemas={cinemas}
                        onScheduleGenerated={fetchHallsAndSessions}
                    />
                </div>
            )}

            <div className="flex items-center gap-2 overflow-x-auto pb-4 mb-6 border-b border-white/5 no-scrollbar">
                {dateTabs.map((dateStr) => {
                    const d = new Date(`${dateStr}T12:00:00Z`);
                    const isSelected = dateStr === selectedDate;
                    return (
                        <button
                            key={dateStr}
                            type="button"
                            onClick={() => setSelectedDate(dateStr)}
                            className={`flex flex-col items-center min-w-[75px] py-2.5 px-3 rounded-xl border transition-all cursor-pointer ${
                                isSelected
                                    ? 'bg-[#ffbd14] text-black border-[#ffbd14] font-black shadow-lg shadow-[#ffbd14]/10'
                                    : 'bg-[#1a1c26] text-gray-400 border-white/5 hover:border-white/10 hover:text-white'
                            }`}
                        >
                            <span className="text-[10px] uppercase tracking-wider font-bold opacity-80">
                                {d.toLocaleDateString('uk-UA', { weekday: 'short', timeZone: 'Europe/Kyiv' })}
                            </span>
                            <span className="text-lg font-black mt-0.5">
                                {d.toLocaleDateString('uk-UA', { day: 'numeric', timeZone: 'Europe/Kyiv' })}
                            </span>
                        </button>
                    );
                })}
            </div>

            {isLoading ? (
                <div className="flex items-center justify-center py-24 bg-[#1a1c26] rounded-2xl border border-white/5">
                    <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                </div>
            ) : (
                <InteractiveTimeline
                    halls={displayHalls}
                    filteredSessions={filteredSessions}
                    selectedDate={selectedDate}
                    onEditSession={handleEditOpen}
                    onDeleteSession={handleDeleteSession}
                    onCreateSessionForSlot={(hallId, timeString) => handleCreateOpen(hallId, timeString)}
                />
            )}

            <SessionFormModal
                isOpen={isModalOpen}
                editingSession={editingSession}
                movies={movies}
                halls={halls}
                initialHallId={modalInitialHallId}
                initialStartTime={modalInitialStartTime}
                onClose={() => setIsModalOpen(false)}
                onSave={handleSaveSession}
                onDelete={handleDeleteSession}
            />
        </div>
    );
};