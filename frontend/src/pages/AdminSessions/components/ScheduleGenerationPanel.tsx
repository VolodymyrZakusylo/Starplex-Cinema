import React, { useEffect, useState } from 'react';
import { Calendar, DollarSign, Film, Sliders, AlertTriangle } from 'lucide-react';
import { moviesApi } from '@/api/movies';
import { sessionsApi } from '@/api/sessions';
import { useToast } from '@/hooks/useToast';
import { getKyivDateString } from '@/utils/date';
import type { CinemaDto } from '@/types/cinemas';
import type { MovieShortDto } from '@/types/movies';

interface ScheduleGenerationPanelProps {
    currentCinemaId: string;
    isSuperAdmin: boolean;
    cinemas: CinemaDto[];
    onScheduleGenerated: () => void;
}

export const ScheduleGenerationPanel: React.FC<ScheduleGenerationPanelProps> = ({
    currentCinemaId,
    isSuperAdmin,
    cinemas,
    onScheduleGenerated
}) => {
    const { showError, showSuccess, confirm } = useToast();
    const [movies, setMovies] = useState<MovieShortDto[]>([]);
    const [selectedCinemaId, setSelectedCinemaId] = useState<string>(currentCinemaId);
    const [targetDate, setTargetDate] = useState<string>(() => getKyivDateString(1));
    const [basePrice, setBasePrice] = useState<number>(150);
    const [selectedMovieIds, setSelectedMovieIds] = useState<string[]>([]);

    const [isLoading, setIsLoading] = useState(true);
    const [isSubmitting, setIsSubmitting] = useState(false);
    const [statusMessage, setStatusMessage] = useState<{ type: 'success' | 'error', text: string } | null>(null);

    useEffect(() => {
        if (currentCinemaId) setSelectedCinemaId(currentCinemaId);
    }, [currentCinemaId]);

    useEffect(() => {
        const loadMovies = async () => {
            setIsLoading(true);
            try {
                const data = await moviesApi.getShortList();
                setMovies(data.filter(m => m.status === 1));
            } catch (err) {
                showError('Не вдалося проаналізувати активні кінорелізи.');
            } finally {
                setIsLoading(false);
            }
        };
        loadMovies();
    }, [showError]);

    const handleToggleMovie = (movieId: string) => {
        setSelectedMovieIds(prev =>
            prev.includes(movieId) ? prev.filter(id => id !== movieId) : [...prev, movieId]
        );
    };

    const handleGenerate = (e: React.FormEvent) => {
        e.preventDefault();

        const todayStr = getKyivDateString(0);
        if (targetDate < todayStr) {
            setStatusMessage({ type: 'error', text: 'Критична помилка: Заборонено генерувати розклад сеансів на минулу дату.' });
            return;
        }

        if (!selectedCinemaId || !targetDate || selectedMovieIds.length === 0) {
            setStatusMessage({ type: 'error', text: 'Будь ласка, заповніть усі параметри та оберіть хоча б один фільм.' });
            return;
        }

        confirm('Згенерувати розклад сеансів для обраного періоду та фільмів?', async () => {
            setIsSubmitting(true);
            setStatusMessage(null);

            try {
                const data = await sessionsApi.generateSchedule({
                    cinemaId: selectedCinemaId,
                    targetDate: `${targetDate}T00:00:00.000Z`,
                    basePrice: basePrice,
                    movieIds: selectedMovieIds
                });

                showSuccess(`Успішно сформовано новий розклад!`);
                setStatusMessage({
                    type: 'success',
                    text: `Успішно згенеровано ${data.count} сеансів для обраного дня.`
                });
                setSelectedMovieIds([]);
                onScheduleGenerated();
            } catch (err: any) {
                setStatusMessage({
                    type: 'error',
                    text: err.response?.data?.message || 'Помилка генерації розкладу. Перевірте, чи не створено вже сеанси на цей день.'
                });
            } finally {
                setIsSubmitting(false);
            }
        });
    };

    if (isLoading) {
        return (
            <div className="flex flex-col items-center justify-center p-16 text-white bg-[#1a1c26] rounded-2xl border border-white/5">
                <div className="w-8 h-8 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                <p className="text-xs text-gray-400 mt-3">Аналіз бази активних релізів...</p>
            </div>
        );
    }

    return (
        <div className="w-full bg-[#1a1c26] border border-white/5 p-6 sm:p-8 rounded-2xl shadow-xl text-white text-xs">
            <div className="flex items-center gap-3 mb-6 border-b border-white/5 pb-4">
                <Sliders className="w-6 h-6 text-[#ffbd14]" />
                <div>
                    <h2 className="text-xl font-black tracking-tight">Смарт-генератор розкладу</h2>
                    <p className="text-xs text-gray-400">Автоматичне циклічне нарізання сеансів з 10:00 до 23:00</p>
                </div>
            </div>

            <form onSubmit={handleGenerate} className="grid grid-cols-1 md:grid-cols-3 gap-6">
                <div className="md:col-span-1 flex flex-col gap-4">
                    {isSuperAdmin && cinemas.length > 0 && (
                        <div>
                            <label className="block text-[10px] uppercase font-bold tracking-wider text-gray-400 mb-1.5">Кінотеатр мережі</label>
                            <select value={selectedCinemaId} onChange={(e) => setSelectedCinemaId(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-3 py-2.5 font-semibold text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer">
                                {cinemas.map(c => <option key={c.id} value={c.id}>{c.city} — {c.name}</option>)}
                            </select>
                        </div>
                    )}

                    <div>
                        <label className="block text-[10px] uppercase font-bold tracking-wider text-gray-400 mb-1.5">Цільова дата розкладу</label>
                        <div className="relative">
                            <Calendar className="w-4 h-4 absolute left-3 top-3 text-gray-500" />
                            <input type="date" value={targetDate} onChange={(e) => setTargetDate(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl pl-10 pr-3 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]" />
                        </div>
                    </div>

                    <div>
                        <label className="block text-[10px] uppercase font-bold tracking-wider text-gray-400 mb-1.5">Базова ціна квитка (₴)</label>
                        <div className="relative">
                            <DollarSign className="w-4 h-4 absolute left-3 top-3 text-gray-500" />
                            <input type="number" min={50} max={1000} value={basePrice} onChange={(e) => setBasePrice(Number(e.target.value))} className="w-full bg-[#111219] border border-white/10 rounded-xl pl-10 pr-3 py-2.5 font-bold text-[#ffbd14] focus:outline-none focus:border-[#ffbd14]" />
                        </div>
                        <span className="text-[10px] text-gray-500 block mt-1.5 leading-relaxed"> Система автоматично адаптує ціни: ранок (-20%), прайм-вечір (+25%), пізні сеанси (+10%).</span>
                    </div>
                </div>

                <div className="md:col-span-2 flex flex-col gap-2">
                    <label className="block text-[10px] uppercase font-bold tracking-wider text-gray-400 mb-0.5">Оберіть фільми для ротації ({selectedMovieIds.length} обрано)</label>
                    <div className="flex-1 min-h-[220px] max-h-[260px] overflow-y-auto border border-white/5 bg-[#111219]/50 rounded-xl p-4 flex flex-col gap-2 no-scrollbar">
                        {movies.length === 0 ? (
                            <p className="text-gray-500 italic text-center py-10">Немає активних фільмів зі статусом «Зараз у кіно».</p>
                        ) : (
                            movies.map(movie => {
                                const isChecked = selectedMovieIds.includes(movie.id);
                                return (
                                    <div key={movie.id} onClick={() => handleToggleMovie(movie.id)} className={`flex items-center justify-between p-3 rounded-xl border transition-all cursor-pointer select-none ${isChecked ? 'bg-[#ffbd14]/10 border-[#ffbd14]/40 text-white shadow' : 'bg-[#111219] border-white/5 hover:border-white/10 text-gray-400'}`}>
                                        <div className="flex items-center gap-3">
                                            <Film className={`w-4 h-4 ${isChecked ? 'text-[#ffbd14]' : 'text-gray-600'}`} />
                                            <div>
                                                <p className={`text-sm font-bold ${isChecked ? 'text-white' : 'text-gray-300'}`}>{movie.title}</p>
                                                <p className="text-[10px] text-gray-500">{movie.genre} • 🕒 {movie.durationInMinutes} хв</p>
                                            </div>
                                        </div>
                                        <div className={`w-4 h-4 rounded-md border flex items-center justify-center text-black text-[10px] font-black transition-all ${isChecked ? 'bg-[#ffbd14] border-[#ffbd14]' : 'border-gray-700'}`}>{isChecked && '✓'}</div>
                                    </div>
                                );
                            })
                        )}
                    </div>
                </div>

                <div className="md:col-span-3 border-t border-white/5 pt-4 flex flex-col gap-4">
                    {statusMessage && (
                        <div className={`p-4 rounded-xl flex items-start gap-3 border text-xs ${statusMessage.type === 'success' ? 'bg-emerald-500/10 border-emerald-500/20 text-emerald-400' : 'bg-red-500/10 border-red-500/20 text-red-400'}`}>
                            {statusMessage.type === 'error' && <AlertTriangle className="w-4 h-4 shrink-0 mt-0.5 text-red-400" />}
                            <p className="leading-relaxed">{statusMessage.text}</p>
                        </div>
                    )}
                    <button type="submit" disabled={isSubmitting || selectedMovieIds.length === 0} className="w-full bg-[#ffbd14] hover:bg-[#e0a40f] disabled:bg-white/5 text-black disabled:text-gray-500 font-black py-3.5 rounded-xl shadow-lg transition-all font-bold uppercase tracking-widest cursor-pointer border-none shadow-[0_4px_20px_rgba(255,189,20,0.15)]">{isSubmitting ? 'Проведення смарт-генерації...' : 'Запустити автоматичну генерацію'}</button>
                </div>
            </form>
        </div>
    );
};