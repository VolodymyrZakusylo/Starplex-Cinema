import React, { useState, useEffect } from 'react';
import { Film, DollarSign, X, Trash2 } from 'lucide-react';
import type { SessionDto, MovieDto, HallDto } from '@/types';
import { isoToKyivDateTimeLocal, kyivDateTimeLocalToUtcIso } from '@/utils/date';

export interface SessionFormPayload {
    id?: string;
    movieId: string;
    hallId: string;
    startTime: string;
    basePrice: number;
}

interface SessionFormModalProps {
    isOpen: boolean;
    editingSession: SessionDto | null;
    movies: MovieDto[];
    halls: HallDto[];
    initialHallId?: string;
    initialStartTime?: string;
    onClose: () => void;
    onSave: (payload: SessionFormPayload) => Promise<void>;
    onDelete?: (id: string) => void;
}

export const SessionFormModal: React.FC<SessionFormModalProps> = ({
    isOpen,
    editingSession,
    movies,
    halls,
    initialHallId,
    initialStartTime,
    onClose,
    onSave,
    onDelete
}) => {
    const [movieId, setMovieId] = useState<string>('');
    const [hallId, setHallId] = useState<string>('');
    const [startTime, setStartTime] = useState<string>('');
    const [basePrice, setBasePrice] = useState<number>(150);
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [movieSearchTerm, setMovieSearchTerm] = useState<string>('');
    const [isMovieDropdownOpen, setIsMovieDropdownOpen] = useState<boolean>(false);
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    useEffect(() => {
        if (!isOpen) return;

        if (editingSession) {
            setMovieId(editingSession.movieId);
            const title = editingSession.movieTitle || (editingSession as any).MovieTitle || '';
            setMovieSearchTerm(title);
            setHallId(editingSession.hallId);
            const timeRaw = editingSession.startTime || (editingSession as any).StartTime;
            setStartTime(timeRaw ? isoToKyivDateTimeLocal(timeRaw) : '');
            setBasePrice(editingSession.basePrice || (editingSession as any).BasePrice || 150);
        } else {
            const defaultMovie = movies[0];
            setMovieId(defaultMovie?.id || '');
            setMovieSearchTerm(defaultMovie?.title || '');
            setHallId(initialHallId && halls.some(h => h.id === initialHallId) ? initialHallId : (halls[0]?.id || ''));
            setStartTime(initialStartTime || '');
            setBasePrice(150);
        }
        setErrorMessage('');
        setIsMovieDropdownOpen(false);
    }, [isOpen, editingSession, initialHallId, initialStartTime, movies, halls]);

    if (!isOpen) return null;

    const searchedMovies = movies.filter(m =>
        m.title.toLowerCase().includes(movieSearchTerm.toLowerCase())
    );

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!movieId) {
            setErrorMessage('Будь ласка, оберіть фільм із списку.');
            return;
        }

        setIsSubmitting(true);
        setErrorMessage('');

        try {
            const payload: SessionFormPayload = {
                id: editingSession?.id || undefined,
                movieId,
                hallId,
                startTime: kyivDateTimeLocalToUtcIso(startTime),
                basePrice: Number(basePrice)
            };

            await onSave(payload);
            onClose();
        } catch (err: any) {
            const apiMsg = err.response?.data?.detail || err.response?.data?.message || err.response?.data?.Message;
            setErrorMessage(apiMsg || err.message || 'Колізія розкладу! Час або клінінг-слот зайнятий.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm animate-fadeIn">
            <div className="w-full max-w-md bg-[#1a1c26] border border-white/10 rounded-2xl shadow-2xl p-6 relative">
                <div className="flex justify-between items-center mb-4">
                    <h2 className="text-xl font-black tracking-tight text-white">
                        {editingSession ? '📝 Редагувати сеанс' : '✨ Створити новий сеанс'}
                    </h2>
                    <button type="button" onClick={onClose} className="p-1 text-gray-400 hover:text-white border-none bg-transparent cursor-pointer">
                        <X className="w-5 h-5" />
                    </button>
                </div>

                {errorMessage && (
                    <div className="mb-4 bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-bold p-3 rounded-xl">
                        {errorMessage}
                    </div>
                )}

                <form onSubmit={handleSubmit} className="space-y-4 text-xs">
                    <div>
                        <label className="block text-gray-400 uppercase font-bold mb-1 flex items-center gap-1">
                            <Film className="w-3 h-3 text-[#ffbd14]" /> Оберіть Фільм
                        </label>
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
                                <button type="button" onClick={() => { setMovieSearchTerm(''); setMovieId(''); }} className="absolute right-3 top-3.5 text-gray-500 hover:text-white border-none bg-transparent cursor-pointer">
                                    <X className="w-4 h-4" />
                                </button>
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
                        <select value={hallId} onChange={(e) => setHallId(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#ffbd14] cursor-pointer">
                            {halls.map(h => <option key={h.id} value={h.id}>{h.name}</option>)}
                        </select>
                    </div>

                    <div>
                        <label className="block text-gray-400 uppercase font-bold mb-1">Час початку</label>
                        <input type="datetime-local" value={startTime} onChange={(e) => setStartTime(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-white focus:outline-none focus:border-[#ffbd14]" />
                    </div>

                    <div>
                        <label className="block text-gray-400 uppercase font-bold mb-1">
                            <DollarSign className="w-3 h-3 text-[#ffbd14] inline" /> Базова ціна квитка (₴)
                        </label>
                        <div className="relative">
                            <input type="number" min="50" max="1000" value={basePrice} onChange={(e) => setBasePrice(Number(e.target.value))} className="w-full bg-[#111219] border border-white/10 rounded-xl pl-10 pr-4 py-3 text-white focus:outline-none focus:border-[#ffbd14]" />
                            <DollarSign className="w-4 h-4 text-gray-500 absolute left-3.5 top-3.5" />
                        </div>
                    </div>

                    <div className="flex items-center justify-between pt-4 border-t border-white/5">
                        {editingSession && onDelete ? (
                            <button
                                type="button"
                                onClick={() => onDelete(editingSession.id)}
                                className="bg-red-500/10 hover:bg-red-500/20 text-red-400 font-bold px-3.5 py-2.5 rounded-xl border border-red-500/20 flex items-center gap-1.5 cursor-pointer transition-all"
                            >
                                <Trash2 className="w-4 h-4" /> Видалити
                            </button>
                        ) : <div />}

                        <div className="flex items-center gap-2">
                            <button type="button" onClick={onClose} className="bg-white/5 hover:bg-white/10 text-white font-bold px-4 py-2.5 rounded-xl border-none cursor-pointer">
                                Скасувати
                            </button>
                            <button type="submit" disabled={isSubmitting} className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-black px-5 py-2.5 rounded-xl uppercase tracking-wider border-none cursor-pointer shadow-lg disabled:opacity-50">
                                {isSubmitting ? 'Збереження...' : 'Зберегти'}
                            </button>
                        </div>
                    </div>
                </form>
            </div>
        </div>
    );
};
