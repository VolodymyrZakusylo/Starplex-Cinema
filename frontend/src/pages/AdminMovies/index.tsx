import React, { useEffect, useState } from 'react';
import { Plus, Trash2, Edit2, Film, Clock, Search, Download, Image, Calendar } from 'lucide-react';
import { moviesApi } from '@/api/movies';
import type { MovieDto, TmdbSearchValue } from '@/types/movies';
import { useToast } from '@/hooks/useToast';
import { MovieFormModal } from './components/MovieFormModal';
import { getMediaUrl } from '@/utils/media';

export const AdminMoviesPage: React.FC = () => {
    const { confirm, showError, showSuccess } = useToast();
    const [movies, setMovies] = useState<MovieDto[]>([]);
    const [isLoading, setIsLoading] = useState<boolean>(true);

    const [isModalOpen, setIsModalOpen] = useState<boolean>(false);
    const [isImportMode, setIsImportMode] = useState<boolean>(true);
    const [tmdbQuery, setTmdbQuery] = useState<string>('');
    const [tmdbResults, setTmdbResults] = useState<TmdbSearchValue[]>([]);
    const [isSearchingTmdb, setIsSearchingTmdb] = useState<boolean>(false);
    const [editingMovie, setEditingMovie] = useState<MovieDto | null>(null);

    const fetchMovies = async () => {
        setIsLoading(true);
        try {
            const data = await moviesApi.getAll();
            setMovies(data);
        } catch (err: any) {
            showError(err.response?.data?.message || 'Не вдалося завантажити каталог фільмів.');
        } finally {
            setIsLoading(false);
        }
    };

    useEffect(() => {
        fetchMovies();
    }, []);

    const handleSearchTmdb = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!tmdbQuery.trim()) return;
        setIsSearchingTmdb(true);
        try {
            const data = await moviesApi.searchTmdb(tmdbQuery);
            setTmdbResults(data);
        } catch (err: any) {
            console.error('TMDB Search error:', err);
            showError('Не вдалося виконати пошук у TMDB.');
        } finally {
            setIsSearchingTmdb(false);
        }
    };

    const handleImportMovie = async (tmdbId: number) => {
        try {
            await moviesApi.importTmdb(tmdbId);
            showSuccess('Фільм успішно імпортовано з медіабази TMDB!');
            setIsModalOpen(false);
            fetchMovies();
        } catch (err: any) {
            showError(err.response?.data?.message || 'Помилка при імпорті фільму.');
        }
    };

    const handleDeleteMovie = (id: string) => {
        confirm('Ви впевнені, що хочете остаточно видалити цей фільм із каталогу мережі?', async () => {
            try {
                await moviesApi.delete(id);
                showSuccess('Фільм успішно видалено.');
                fetchMovies();
            } catch (err: any) {
                showError(err.response?.data?.message || 'Не вдалося видалити фільм.');
            }
        });
    };

    const getMoviePoster = (movie: MovieDto) => {
        const path = movie.effectivePosterUrl || movie.posterUrl;
        return getMediaUrl(path);
    };

    return (
        <div className="w-full select-none">
            <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-6 mb-10 border-b border-white/5 pb-6">
                <div>
                    <h1 className="text-3xl font-black tracking-tight flex items-center gap-3">
                        <Film className="text-[#ffbd14] w-8 h-8" /> Каталог фільмів
                    </h1>
                    <p className="text-gray-400 text-sm mt-1">Керування медіатекою фільмів StarPlex</p>
                </div>

                <div className="flex items-center gap-3 w-full sm:w-auto text-xs font-bold">
                    <button onClick={() => { setIsImportMode(true); setTmdbQuery(''); setTmdbResults([]); setIsModalOpen(true); }} className="bg-[#1a1c26] hover:bg-[#232635] text-white border border-white/10 px-5 py-3 rounded-xl transition-all uppercase tracking-wider flex items-center justify-center gap-2 cursor-pointer border-none"><Download className="w-4 h-4 text-[#ffbd14]" /> Імпорт з TMDB</button>
                    <button onClick={() => { setEditingMovie(null); setIsImportMode(false); setIsModalOpen(true); }} className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black px-5 py-3 rounded-xl transition-all shadow-lg flex items-center justify-center gap-2 uppercase tracking-wider cursor-pointer border-none"><Plus className="w-4 h-4 stroke-[3]" /> Додати вручну</button>
                </div>
            </div>

            {isLoading ? (
                <div className="flex items-center justify-center py-24"><div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div></div>
            ) : movies.length === 0 ? (
                <div className="text-center py-20 bg-[#1a1c26] rounded-2xl border border-white/5 p-8"><p className="text-gray-400 text-sm">Каталог порожній.</p></div>
            ) : (
                <div className="grid grid-cols-2 sm:grid-cols-3 md:grid-cols-4 lg:grid-cols-5 xl:grid-cols-6 gap-6">
                    {movies.map((movie) => (
                        <div key={movie.id} className="bg-[#1a1c26] border border-white/5 rounded-2xl overflow-hidden shadow-xl group hover:border-[#ffbd14]/30 transition-all flex flex-col relative">
                            <div className="aspect-[2/3] w-full bg-[#111219] relative overflow-hidden border-b border-white/5">
                                <img 
                                    src={getMoviePoster(movie)} 
                                    alt={movie.title} 
                                    className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-300" 
                                    onError={(e) => { (e.target as HTMLImageElement).src = 'https://placehold.co/400x600?text=No+Poster'; }} 
                                />
                                <div className="absolute top-2 right-2 opacity-0 group-hover:opacity-100 transition-opacity flex items-center gap-1 z-10">
                                    <button onClick={() => { setEditingMovie(movie); setIsImportMode(false); setIsModalOpen(true); }} className="p-2 bg-[#111219]/80 backdrop-blur-md hover:bg-[#ffbd14] hover:text-black rounded-lg text-gray-300 transition-colors cursor-pointer border-none"><Edit2 className="w-3.5 h-3.5" /></button>
                                    <button onClick={() => handleDeleteMovie(movie.id)} className="p-2 bg-[#111219]/80 backdrop-blur-md hover:bg-red-500 hover:text-white rounded-lg text-red-400 transition-colors cursor-pointer border-none"><Trash2 className="w-3.5 h-3.5" /></button>
                                </div>
                            </div>
                            <div className="p-4 flex-1 flex flex-col justify-between">
                                <h3 className="font-bold text-sm tracking-tight text-white line-clamp-2 mb-2 group-hover:text-[#ffbd14] transition-colors">{movie.title}</h3>
                                <div className="flex items-center justify-between text-xs text-gray-400 border-t border-white/5 pt-2 mt-auto">
                                    <span className="flex items-center gap-1"><Clock className="w-3.5 h-3.5 text-[#ffbd14]" /> {movie.durationInMinutes} хв</span>
                                    {movie.ageRating && <span className="bg-white/5 px-1.5 py-0.5 rounded text-[10px] text-gray-300 font-bold border border-white/5">{movie.ageRating}</span>}
                                </div>
                            </div>
                        </div>
                    ))}
                </div>
            )}

            {isModalOpen && (
                isImportMode ? (
                    <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
                        <div className="w-full max-w-lg bg-[#1a1c26] border border-white/10 rounded-2xl shadow-2xl p-6 relative flex flex-col max-h-[90vh]">
                            <h2 className="text-xl font-black tracking-tight mb-4 text-white">🎬 Швидкий імпорт з TMDB</h2>
                            <form onSubmit={handleSearchTmdb} className="flex gap-2 mb-4">
                                <div className="relative flex-1">
                                    <input type="text" value={tmdbQuery} onChange={(e) => setTmdbQuery(e.target.value)} placeholder="Введіть назву фільму..." className="w-full bg-[#111219] border border-white/10 rounded-xl pl-10 pr-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14]" />
                                    <Search className="w-4 h-4 text-gray-500 absolute left-3.5 top-3.5" />
                                </div>
                                <button type="submit" disabled={isSearchingTmdb} className="bg-[#ffbd14] hover:bg-[#e0a40f] text-black font-bold text-xs px-5 py-3 rounded-xl uppercase disabled:opacity-50 tracking-wider cursor-pointer border-none">{isSearchingTmdb ? 'Пошук...' : 'Знайти'}</button>
                            </form>

                            <div className="flex-1 overflow-y-auto space-y-2 pr-1 no-scrollbar min-h-[200px]">
                                {tmdbResults.map((item) => (
                                    <div key={item.id} className="flex items-center justify-between p-3 bg-[#111219] border border-white/5 rounded-xl">
                                        <div className="flex items-center gap-3">
                                            <div className="w-10 h-14 bg-[#1a1c26] rounded-md overflow-hidden flex-shrink-0 border border-white/5 flex items-center justify-center text-gray-500">
                                                {(item as any).posterUrl || (item as any).PosterUrl || (item as any).posterPath ? (
                                                    <img src={(item as any).posterUrl || (item as any).PosterUrl || (item as any).posterPath} alt={item.title} className="w-full h-full object-cover" />
                                                ) : (
                                                    <Image className="w-4 h-4 text-gray-600" />
                                                )}
                                            </div>
                                            <div>
                                                <h4 className="font-bold text-sm text-white line-clamp-1">{item.title}</h4>
                                                <p className="text-xs text-gray-400 flex items-center gap-1 mt-0.5"><Calendar className="w-3 h-3" /> {item.releaseDate || 'Невідомо'}</p>
                                            </div>
                                        </div>
                                        <button 
                                            onClick={() => {
                                                const realTmdbId = item.id || (item as any).tmdbId || (item as any).TmdbId;
                                                if (!realTmdbId) {
                                                    showError("Помилка: Не вдалося зчитати TMDB ID фільму.");
                                                    return;
                                                }
                                                handleImportMovie(realTmdbId);
                                            }}
                                            className="bg-[#ffbd14]/10 hover:bg-[#ffbd14] text-[#ffbd14] hover:text-black font-black text-[10px] px-3 py-2 rounded-lg uppercase tracking-wider transition-colors cursor-pointer border-none"
                                        >
                                            Імпорт
                                        </button>
                                    </div>
                                ))}
                            </div>
                            <div className="flex items-center justify-between pt-4 border-t border-white/5 mt-4 text-xs font-bold">
                                <button type="button" onClick={() => setIsImportMode(false)} className="text-[#ffbd14] hover:underline bg-transparent border-none cursor-pointer">Перейти на ручне введення</button>
                                <button type="button" onClick={() => setIsModalOpen(false)} className="bg-white/5 text-white px-4 py-2.5 rounded-xl cursor-pointer border-none">Закрити</button>
                            </div>
                        </div>
                    </div>
                ) : (
                    <MovieFormModal editingMovie={editingMovie} onClose={() => setIsModalOpen(false)} onRefresh={fetchMovies} />
                )
            )}
        </div>
    );
};