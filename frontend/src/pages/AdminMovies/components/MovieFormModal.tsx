import React, { useState, useEffect } from 'react';
import { Clock, Tv, Upload, Link } from 'lucide-react';
import { moviesApi } from '@/api/movies';
import type { MovieDto } from '@/types/movies';
import { useToast } from '@/hooks/useToast';

interface MovieFormModalProps {
    editingMovie: MovieDto | null;
    onClose: () => void;
    onRefresh: () => void;
}

export const MovieFormModal: React.FC<MovieFormModalProps> = ({ editingMovie, onClose, onRefresh }) => {
    const { showError, showSuccess } = useToast();
    const [movieTitle, setMovieTitle] = useState('');
    const [description, setDescription] = useState('');
    const [durationInMinutes, setDurationInMinutes] = useState(120);
    const [backdropUrl, setBackdropUrl] = useState('');
    const [genre, setGenre] = useState('');
    const [trailerUrl, setTrailerUrl] = useState('');
    const [ageRating, setAgeRating] = useState('PG-13');
    const [movieStatus, setMovieStatus] = useState(0);

    const [posterType, setPosterType] = useState<'file' | 'url'>('file');
    const [posterUrl, setPosterUrl] = useState('');
    const [posterFile, setPosterFile] = useState<File | null>(null);
    const [imagePreview, setImagePreview] = useState('');
    const [isSaving, setIsSaving] = useState(false);

    useEffect(() => {
        if (editingMovie) {
            setMovieTitle(editingMovie.title || '');
            setDescription(editingMovie.description || '');
            setDurationInMinutes(editingMovie.durationInMinutes || 120);
            setBackdropUrl(editingMovie.backdropUrl || '');
            setGenre(editingMovie.genre || '');
            setTrailerUrl(editingMovie.trailerUrl || '');
            setAgeRating(editingMovie.ageRating || 'PG-13');
            setMovieStatus(editingMovie.status ?? 0);
            
            const basePosterUrl = (editingMovie.posterUrl && editingMovie.posterUrl !== 'undefined') ? editingMovie.posterUrl : '';
            setPosterUrl(basePosterUrl);

            const rawPoster = editingMovie.effectivePosterUrl || editingMovie.posterUrl || '';
            const validRawPoster = (rawPoster && rawPoster !== 'undefined') ? rawPoster : '';
            
            const fullPosterUrl = validRawPoster.startsWith('/uploads')
                ? `http://localhost:5108${validRawPoster}`
                : validRawPoster;

            setImagePreview(fullPosterUrl);
            setPosterType(validRawPoster.startsWith('http') ? 'url' : 'file');
        }
    }, [editingMovie]);

    const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        if (e.target.files && e.target.files[0]) {
            const file = e.target.files[0];
            setPosterFile(file);
            setImagePreview(URL.createObjectURL(file));
        }
    };

    const handleSave = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!movieTitle.trim()) return showError('Назва фільму обов’язкова');

        setIsSaving(true);
        const formData = new FormData();
        const statusMap = ['ComingSoon', 'NowShowing', 'Archived'];
        const stringStatus = statusMap[movieStatus] || 'ComingSoon';

        formData.append('command.Title', movieTitle.trim());
        formData.append('command.Description', description.trim());
        formData.append('command.DurationInMinutes', durationInMinutes.toString());
        formData.append('command.BackdropUrl', backdropUrl.trim());
        formData.append('command.Genre', genre.trim());
        formData.append('command.TrailerUrl', trailerUrl.trim());
        formData.append('command.AgeRating', ageRating);
        formData.append('command.Status', stringStatus);

        let calculatedPosterUrl = '';
        const currentStateUrl = (posterUrl && posterUrl !== 'undefined') ? posterUrl.trim() : '';
        const dbMovieUrl = (editingMovie?.posterUrl && editingMovie.posterUrl !== 'undefined') ? editingMovie.posterUrl : '';
        const dbEffectiveUrl = (editingMovie?.effectivePosterUrl && editingMovie.effectivePosterUrl !== 'undefined') ? editingMovie.effectivePosterUrl : '';

        if (posterType === 'file') {
            if (posterFile) {
                formData.append('posterFile', posterFile);
                calculatedPosterUrl = `/uploads/movies/${posterFile.name}`;
            } else {
                calculatedPosterUrl = dbMovieUrl || dbEffectiveUrl || '/uploads/movies/placeholder.png';
            }
        } else {
            calculatedPosterUrl = currentStateUrl || dbMovieUrl || dbEffectiveUrl || '/uploads/movies/placeholder.png';
        }

        if (calculatedPosterUrl.includes('undefined')) {
            calculatedPosterUrl = dbEffectiveUrl || '/uploads/movies/placeholder.png';
        }

        formData.append('command.PosterUrl', calculatedPosterUrl);
        formData.append('command.posterUrl', calculatedPosterUrl);
        formData.append('PosterUrl', calculatedPosterUrl);
        formData.append('posterUrl', calculatedPosterUrl);

        try {
            if (editingMovie) {
                formData.append('command.Id', editingMovie.id);
                await moviesApi.update(editingMovie.id, formData);
                showSuccess('Дані фільму успішно оновлено!');
            } else {
                await moviesApi.create(formData);
                showSuccess('Новий фільм додано до каталогу!');
            }
            onRefresh();
            onClose();
        } catch (err: any) {
            const msg = err.response?.data?.errors?.['command.PosterUrl']?.[0] || 'Помилка збереження';
            showError(msg);
        } finally {
            setIsSaving(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
            <div className="w-full max-w-lg bg-[#1a1c26] border border-white/10 rounded-2xl shadow-2xl p-6 relative flex flex-col max-h-[90vh] text-xs">
                <h2 className="text-xl font-black tracking-tight mb-4 text-white">
                    {editingMovie ? '📝 Редагувати фільм' : '✨ Додати фільм'}
                </h2>

                <form onSubmit={handleSave} className="space-y-4 overflow-y-auto pr-1 no-scrollbar flex-1">
                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <div>
                            <label className="block text-gray-400 font-bold uppercase mb-1">Назва</label>
                            <input type="text" value={movieTitle} onChange={(e) => setMovieTitle(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]" required />
                        </div>
                        <div>
                            <label className="block text-gray-400 font-bold uppercase mb-1 flex items-center gap-1"><Clock className="w-3 h-3" /> Хв.</label>
                            <input type="number" min="1" value={durationInMinutes} onChange={(e) => setDurationInMinutes(Number(e.target.value))} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]" required />
                        </div>
                    </div>

                    <div>
                        <label className="block text-gray-400 font-bold uppercase mb-1">Опис</label>
                        <textarea value={description} onChange={(e) => setDescription(e.target.value)} rows={3} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14] resize-none" />
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-3 gap-4">
                        <div>
                            <label className="block text-gray-400 font-bold uppercase mb-1">Жанр</label>
                            <input type="text" value={genre} onChange={(e) => setGenre(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]" />
                        </div>
                        <div>
                            <label className="block text-gray-400 font-bold uppercase mb-1">Рейтинг</label>
                            <select value={ageRating} onChange={(e) => setAgeRating(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none">
                                <option value="G">G (0+)</option>
                                <option value="PG">PG (6+)</option>
                                <option value="PG-13">PG-13 (12+)</option>
                                <option value="R">R (16+)</option>
                                <option value="NC-17">NC-17 (18+)</option>
                            </select>
                        </div>
                        <div>
                            <label className="block text-gray-400 font-bold uppercase mb-1">Статус</label>
                            <select value={movieStatus} onChange={(e) => setMovieStatus(Number(e.target.value))} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none">
                                <option value={0}>Анонс</option>
                                <option value={1}>В прокаті</option>
                                <option value={2}>Архів</option>
                            </select>
                        </div>
                    </div>

                    <div className="grid grid-cols-1 sm:grid-cols-2 gap-4">
                        <div>
                            <label className="block text-gray-400 font-bold uppercase mb-1 flex items-center gap-1"><Tv className="w-3 h-3" /> Трейлер URL</label>
                            <input type="text" value={trailerUrl} onChange={(e) => setTrailerUrl(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white" />
                        </div>
                        <div>
                            <label className="block text-gray-400 font-bold uppercase mb-1">Тло (URL)</label>
                            <input type="text" value={backdropUrl} onChange={(e) => setBackdropUrl(e.target.value)} className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white" />
                        </div>
                    </div>

                    <div>
                        <label className="block text-gray-400 font-bold uppercase mb-2">Обкладинка (Постер)</label>
                        <div className="grid grid-cols-2 gap-2 p-1 bg-[#111219] rounded-xl border border-white/5 mb-3">
                            <button type="button" onClick={() => setPosterType('file')} className={`flex items-center justify-center gap-2 py-2 text-xs font-bold rounded-lg border-none cursor-pointer transition-all ${posterType === 'file' ? 'bg-[#ffbd14] text-black' : 'text-gray-400 hover:text-white'}`}><Upload className="w-3.5 h-3.5" /> Файл</button>
                            <button type="button" onClick={() => setPosterType('url')} className={`flex items-center justify-center gap-2 py-2 text-xs font-bold rounded-lg border-none cursor-pointer transition-all ${posterType === 'url' ? 'bg-[#ffbd14] text-black' : 'text-gray-400 hover:text-white'}`}><Link className="w-3.5 h-3.5" /> URL</button>
                        </div>

                        {posterType === 'file' ? (
                            <div className="flex items-center gap-4 p-4 bg-[#111219] border border-dashed border-white/10 rounded-xl relative">
                                <input type="file" accept="image/*" onChange={handleFileChange} className="absolute inset-0 w-full h-full opacity-0 cursor-pointer" />
                                <div className="w-16 h-20 bg-[#1a1c26] rounded-lg overflow-hidden flex-shrink-0 border border-white/5 flex items-center justify-center">
                                    {imagePreview ? <img src={imagePreview} alt="Preview" className="w-full h-full object-cover" /> : <Upload className="w-6 h-6 text-gray-600" />}
                                </div>
                                <div>
                                    <p className="text-xs font-bold text-white">Змінити файл</p>
                                    <p className="text-[11px] text-gray-400">PNG, JPG до 5 МБ</p>
                                    {posterFile && <p className="text-[10px] text-[#ffbd14] mt-1 truncate max-w-[200px]">{posterFile.name}</p>}
                                </div>
                            </div>
                        ) : (
                            <input type="text" value={posterUrl} onChange={(e) => { setPosterUrl(e.target.value); setImagePreview(e.target.value); }} placeholder="https://..." className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-[#ffbd14]" />
                        )}
                    </div>

                    <div className="flex items-center justify-end gap-3 pt-4 border-t border-white/5 sticky bottom-0 bg-[#1a1c26] w-full">
                        <button type="button" onClick={onClose} className="bg-white/5 text-white font-bold text-xs px-4 py-2.5 rounded-xl border-none cursor-pointer">Скасувати</button>
                        <button type="submit" disabled={isSaving} className="bg-[#ffbd14] text-black font-black text-xs px-5 py-2.5 rounded-xl uppercase tracking-wider disabled:opacity-50 border-none cursor-pointer">{isSaving ? 'Збереження...' : 'Зберегти'}</button>
                    </div>
                </form>
            </div>
        </div>
    );
};