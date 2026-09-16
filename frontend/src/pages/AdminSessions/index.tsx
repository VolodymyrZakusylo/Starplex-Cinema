import { useEffect, useState } from 'react';
import { useAuthStore } from '@/store/authStore';
import { useSearchParams } from 'react-router-dom';
import { Plus, Clock, Film, DollarSign, X } from 'lucide-react';
import { moviesApi } from '@/api/movies';
import { cinemasApi } from '@/api/cinemas';
import { hallsApi } from '@/api/halls';
import { sessionsApi } from '@/api/sessions';
import { useToast } from '@/hooks/useToast';

import { getKyivDateString } from '@/utils/date';
import { ManualScheduleManager } from './components/ManualScheduleManager';
import { ScheduleGenerationPanel } from './components/ScheduleGenerationPanel';
import { InteractiveTimeline } from './components/InteractiveTimeline';

import type { SessionDto, MovieDto, HallDto, CinemaDto } from '@/types';

export const AdminSessionsPage: React.FC = () => {
    const { user } = useAuthStore();
    const isSuperAdmin = user?.roles.includes('SuperAdmin');
    const [searchParams, setSearchParams] = useSearchParams();

    const cinemaIdFromUrl = searchParams.get('cinemaId');
    const hallIdFromUrl = searchParams.get('hallId');

    const [activeTab, setActiveTab] = useState<'view' | 'generate'>('view');

    const [selectedCinemaId, setSelectedCinemaId] = useState<string>('');
    const [cinemas, setCinemas] = useState<CinemaDto[]>([]);
    const [halls, setHalls] = useState<HallDto[]>([]);
    const [movies, setMovies] = useState<MovieDto[]>([]);
    const [sessions, setSessions] = useState<SessionDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [selectedDate, setSelectedDate] = useState<string>(() => getKyivDateString(0));
    const [dateTabs, setDateTabs] = useState<string[]>([]);

    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
    const [editingSession, setEditingSession] = useState<SessionDto | null>(null);
    const [movieId, setMovieId] = useState<string>('');
    const [hallId, setHallId] = useState<string>('');
    const [startTime, setStartTime] = useState<string>('');
    const [basePrice, setBasePrice] = useState<number>(120);
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [movieSearchTerm, setMovieSearchTerm] = useState<string>('');
    const [isMovieDropdownOpen, setIsMovieDropdownOpen] = useState<boolean>(false);

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
            setHalls(hallsData.filter(h => h.isActive));

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

    const clearHallFilter = () => {
        setSearchParams(selectedCinemaId ? { cinemaId: selectedCinemaId } : {});
    };

    const filteredSessions = sessions.filter(s => {
        const timeRaw = s.startTime || (s as any).StartTime;
        const currentStatus = s.status ?? (s as any).Status;

        if (!timeRaw) return false;
        const isActive = (currentStatus as any) === 'Active' || (currentStatus as any) === 0;

        const sessionKyivDate = new Intl.DateTimeFormat('en-CA', { timeZone: 'Europe/Kyiv' }).format(new Date(timeRaw));
        return sessionKyivDate === selectedDate && isActive;
    });

    const searchedMovies = movies.filter(m =>
        m.title.toLowerCase().includes(movieSearchTerm.toLowerCase())
    );

    const handleCreateOpen = () => {
        if (halls.length === 0 || movies.length === 0) {
            showError('Для створення сеансу в базі повинні бути активні зали та фільми!');
            return;
        }
        setEditingSession(null);
        setMovieId(movies[0].id);
        setMovieSearchTerm(movies[0].title);
        setHallId(hallIdFromUrl && halls.some(h => h.id === hallIdFromUrl) ? hallIdFromUrl : halls[0].id);
        setStartTime(`${selectedDate}T18:00`);
        setBasePrice(150);
        setErrorMessage('');
        setIsMovieDropdownOpen(false);
        setIsModalOpen(true);
    };

    const handleEditOpen = (session: SessionDto) => {
        setEditingSession(session);
        setMovieId(session.movieId);
        const title = session.movieTitle || (session as any).MovieTitle;
        setMovieSearchTerm(title || '');
        setHallId(session.hallId);
        const timeRaw = session.startTime || (session as any).StartTime;
        setStartTime(timeRaw ? timeRaw.substring(0, 16) : '');
        setBasePrice(session.basePrice || (session as any).BasePrice);
        setErrorMessage('');
        setIsMovieDropdownOpen(false);
        setIsModalOpen(true);
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!movieId) {
            showError('Будь ласка, оберіть фільм із списку.');
            return;
        }

        const payload = {
            id: editingSession?.id || undefined,
            cinemaId: selectedCinemaId,
            movieId,
            hallId,
            startTime: (() => {
                const localDate = new Date(startTime);
                const utcDate = new Date(Date.UTC(
                    localDate.getFullYear(),
                    localDate.getMonth(),
                    localDate.getDate(),
                    localDate.getHours(),
                    localDate.getMinutes()
                ));
                return utcDate.toISOString();
            })(),
            basePrice: Number(basePrice)
        };

        try {
            if (editingSession) {
                await sessionsApi.update(editingSession.id, payload);
                showSuccess('Сеанс успішно відредаговано.');
            } else {
                await sessionsApi.create(payload);
                showSuccess('Новий сеанс успішно внесено до сітки залу.');
            }
            setIsModalOpen(false);
            fetchHallsAndSessions();
        } catch (err: any) {
            setErrorMessage(err.response?.data?.message || 'Колізія розкладу! Час або клінінг-слот зайнятий.');
        }
    };

    const handleDelete = async (id: string) => {
        confirm('Скасувати цей сеанс? Клієнтам автоматично повернуться кошти на картки.', async () => {
            try {
                await sessionsApi.delete(id);
                showSuccess('Сеанс скасовано, кошти відправлено на повернення.');
                fetchHallsAndSessions();
            } catch (err: any) {
                showError(err.response?.data?.message || 'Не вдалося видалити сеанс.');
            }
        });
    };

    const formatTime = (isoString: string) => {
        if (!isoString) return '00:00';
        return new Date(isoString).toLocaleTimeString('uk-UA', {
            hour: '2-digit',
            minute: '2-digit',
            timeZone: 'Europe/Kyiv'
        });
    };

    return (
        <div className="w-full select-none">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6 mb-6 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <Clock className="text-[#ffbd14] w-8 h-8" /> Управління розкладом
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">Ручне редагування таймлайнів та інтелектуальна автоматична генерація</p>
                </div>

                <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-4 w-full sm:w-auto">
                    {isSuperAdmin && cinemas.length > 0 && (
                        <select
                            value={selectedCinemaId}
                            onChange={(e) => handleCinemaChange(e.target.value)}
                            className="bg-[#1a1c26] border border-white/10 rounded-xl px-4 py-2.5 text-sm font-semibold text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer"
                        >
                            {cinemas.map(c => (
                                <option key={c.id} value={c.id}>{c.city} — {c.name}</option>
                            ))}
                        </select>
                    )}

                    <button
                        onClick={handleCreateOpen}
                        className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black text-xs font-black px-5 py-3 rounded-xl transition-all shadow-lg flex items-center justify-center gap-2 uppercase tracking-wider border-none cursor-pointer"
                    >
                        <Plus className="w-4 h-4 stroke-[3]" /> Додати сеанс
                    </button>
                </div>
            </div>

            <div className="flex gap-4 mb-6 border-b border-white/5 pb-4">
                <button
                    onClick={() => setActiveTab('view')}
                    className={`px-4 py-2 rounded-xl text-xs font-black uppercase tracking-wider transition-all border-none cursor-pointer ${activeTab === 'view' ? 'bg-white/10 text-[#ffbd14]' : 'text-gray-400 hover:text-white bg-transparent'}`}
                >
                    🗓️ Поточна сітка залів
                </button>
                <button
                    onClick={() => setActiveTab('generate')}
                    className={`px-4 py-2 rounded-xl text-xs font-black uppercase tracking-wider transition-all border-none cursor-pointer ${activeTab === 'generate' ? 'bg-[#ffbd14]/10 text-[#ffbd14] border border-[#ffbd14]/20' : 'text-gray-400 hover:text-white bg-transparent'}`}
                >
                    ⚡ Розумна автогенерація
                </button>
            </div>

            {activeTab === 'view' ? (
                <>
                    <div className="flex items-center gap-2 overflow-x-auto pb-4 mb-8 border-b border-white/5 no-scrollbar">
                        {dateTabs.map((dateStr) => {
                            const d = new Date(dateStr);
                            const isSelected = dateStr === selectedDate;
                            return (
                                <button
                                    key={dateStr}
                                    onClick={() => setSelectedDate(dateStr)}
                                    className={`flex flex-col items-center min-w-[70px] py-3 rounded-xl border transition-all cursor-pointer ${isSelected ? 'bg-[#ffbd14] text-black border-[#ffbd14] font-bold shadow-md' : 'bg-[#1a1c26] text-gray-400 border-white/5 hover:border-white/10'}`}
                                >
                                    <span className="text-[10px] uppercase tracking-wider opacity-75">{d.toLocaleDateString('uk-UA', { weekday: 'short' })}</span>
                                    <span className="text-lg font-black mt-0.5">{d.toLocaleDateString('uk-UA', { day: 'numeric' })}</span>
                                </button>
                            );
                        })}
                    </div>

                    {isLoading ? (
                        <div className="flex items-center justify-center py-24">
                            <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                        </div>
                    ) : (
                        <div className="space-y-10">
                            <InteractiveTimeline
                                halls={halls}
                                filteredSessions={filteredSessions}
                                selectedDate={selectedDate}
                                onScheduleUpdated={fetchHallsAndSessions}
                                onDeleteSession={handleDelete}
                                onEditSession={handleEditOpen}
                            />
                            <ManualScheduleManager
                                halls={halls}
                                filteredSessions={filteredSessions}
                                hallIdFromUrl={hallIdFromUrl}
                                clearHallFilter={clearHallFilter}
                                handleEditOpen={handleEditOpen}
                                handleDelete={handleDelete}
                                formatTime={formatTime}
                            />
                        </div>
                    )}
                </>
            ) : (
                <ScheduleGenerationPanel
                    currentCinemaId={selectedCinemaId}
                    isSuperAdmin={isSuperAdmin || false}
                    cinemas={cinemas}
                    onScheduleGenerated={fetchHallsAndSessions}
                />
            )}

            {isModalOpen && (
                <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
                    <div className="w-full max-w-md bg-[#1a1c26] border border-white/10 rounded-2xl shadow-2xl p-6 relative">
                        <h2 className="text-xl font-black tracking-tight mb-4 text-white">
                            {editingSession ? '📝 Редагувати сеанс' : '✨ Створити новий сеанс'}
                        </h2>

                        {errorMessage && (
                            <div className="mb-4 bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-bold p-3 rounded-xl">
                                {errorMessage}
                            </div>
                        )}

                        <form onSubmit={handleSave} className="space-y-4 text-xs">
                            <div>
                                <label className="block text-gray-400 uppercase font-bold mb-1 flex items-center gap-1"><Film className="w-3 h-3" /> Оберіть Фільм</label>
                                <div className="relative">
                                    <input
                                        type="text"
                                        value={movieSearchTerm}
                                        onFocus={() => setIsMovieDropdownOpen(true)}
                                        onChange={(e) => { setMovieSearchTerm(e.target.value); setIsMovieDropdownOpen(true); }}
                                        placeholder="Почніть вводити назву фільму..."
                                        className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#ffbd14]"
                                    />
                                    {movieSearchTerm && (
                                        <button type="button" onClick={() => { setMovieSearchTerm(''); setMovieId(''); }} className="absolute right-3 top-3.5 text-gray-500 hover:text-white border-none bg-transparent cursor-pointer"><X className="w-4 h-4" /></button>
                                    )}
                                </div>

                                {isMovieDropdownOpen && (
                                    <div className="absolute z-50 w-full max-w-[380px] mt-1 bg-[#111219] border border-white/10 rounded-xl max-h-60 overflow-y-auto shadow-2xl divide-y divide-white/5 no-scrollbar">
                                        {searchedMovies.length === 0 ? (
                                            <div className="p-3 text-xs text-gray-500 italic text-center">Фільмів не знайдено</div>
                                        ) : (
                                            searchedMovies.map(m => (
                                                <button
                                                    key={m.id}
                                                    type="button"
                                                    onClick={() => { setMovieId(m.id); setMovieSearchTerm(m.title); setIsMovieDropdownOpen(false); }}
                                                    className={`w-full text-left px-4 py-3 text-xs flex justify-between items-center border-none cursor-pointer ${m.id === movieId ? 'bg-[#ffbd14] text-black font-bold' : 'text-gray-300 bg-transparent hover:bg-white/5'}`}
                                                >
                                                    <span className="truncate">{m.title}</span>
                                                    <span className="text-[10px] font-mono opacity-60 ml-2">{m.durationInMinutes} хв</span>
                                                </button>
                                            ))
                                        )}
                                    </div>
                                )}
                            </div>

                            <div>
                                <label className="block text-gray-400 uppercase font-bold mb-1">Зал кінотеатру</label>
                                <select value={hallId} onChange={(e) => setHallId(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#ffbd14]">
                                    {halls.map(h => <option key={h.id} value={h.id}>{h.name}</option>)}
                                </select>
                            </div>

                            <div>
                                <label className="block text-gray-400 uppercase font-bold mb-1">Час початку</label>
                                <input type="datetime-local" value={startTime} onChange={(e) => setStartTime(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#ffbd14]" />
                            </div>

                            <div>
                                <label className="block text-gray-400 uppercase font-bold mb-1"><DollarSign className="w-3 h-3" /> Базова ціна квитка (₴)</label>
                                <div className="relative">
                                    <input type="number" min="50" max="1000" value={basePrice} onChange={(e) => setBasePrice(Number(e.target.value))} className="w-full bg-[#111219] border border-white/10 rounded-xl pl-10 pr-4 py-3 text-white focus:outline-none focus:border-[#ffbd14]" />
                                    <DollarSign className="w-4 h-4 text-gray-500 absolute left-3.5 top-3.5" />
                                </div>
                            </div>

                            <div className="flex items-center justify-end gap-3 pt-4 border-t border-white/5">
                                <button type="button" onClick={() => setIsModalOpen(false)} className="bg-white/5 hover:bg-white/10 text-white font-bold px-4 py-3 rounded-xl border-none cursor-pointer">Скасувати</button>
                                <button type="submit" className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-black px-5 py-3 rounded-xl uppercase tracking-wider border-none cursor-pointer">Зберегти розклад</button>
                            </div>
                        </form>
                    </div>
                </div>
            )}
        </div>
    );
};