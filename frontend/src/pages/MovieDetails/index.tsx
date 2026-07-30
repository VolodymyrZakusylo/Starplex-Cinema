import React, { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import { moviesApi } from '@/api/movies';
import { useCinemaStore } from '@/store/cinemaStore';
import { MovieTrailer } from './components/MovieTrailer';
import { MovieSchedule } from './components/MovieSchedule';
import { useToast } from '@/hooks/useToast';
import type { MovieDto } from '@/types/index';

export const MovieDetailsPage: React.FC = () => {
    const { id: slugWithId } = useParams<{ id: string }>();
    const navigate = useNavigate();
    const { showError } = useToast();
    const { selectedCinemaId } = useCinemaStore();

    const [movie, setMovie] = useState<MovieDto | null>(null);
    const [isLoading, setIsLoading] = useState<boolean>(true);
    const [activeDay, setActiveDay] = useState<string>("");

    const getYouTubeEmbedUrl = (url: string | null | undefined) => {
        if (!url) return null;
        const regExp = /^.*(youtu.be\/|v\/|u\/\w\/|embed\/|watch\?v=|\&v=)([^#\&\?]*).*/;
        const match = url.match(regExp);
        return match && match[2].length === 11 ? match[2] : null;
    };

    useEffect(() => {
        const fetchMovieDetails = async () => {
            if (!slugWithId) return;
            const realId = slugWithId.includes('--') ? slugWithId.split('--').pop() : slugWithId;
            if (!realId) return;

            setIsLoading(true);
            try {
                const data = await moviesApi.getById(realId);
                setMovie(data);

                const branchSessions = data.sessions?.filter(s => s.cinemaId === selectedCinemaId && s.status === 0 && new Date(s.startTime) > new Date()) || [];
                if (branchSessions.length > 0) {
                    const firstSessionDate = new Date(branchSessions[0].startTime);
                    const fullLabel = firstSessionDate.toLocaleDateString('uk-UA', { weekday: 'long', day: 'numeric', month: 'long' });
                    setActiveDay(fullLabel.charAt(0).toUpperCase() + fullLabel.slice(1));
                }
            } catch (err) {
                showError('Не вдалося завантажити розширену інформацію про фільм.');
            } finally {
                setIsLoading(false);
            }
        };
        fetchMovieDetails();
    }, [slugWithId, selectedCinemaId, showError]);

    if (isLoading) {
        return (
            <div className="flex flex-col items-center justify-center py-40 min-h-[70vh] text-white">
                <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                <p className="text-gray-400 text-xs font-bold uppercase tracking-wider mt-4">Синхронізація медіатеки StarPlex...</p>
            </div>
        );
    }

    if (!movie) {
        return (
            <div className="text-center py-40 max-w-md mx-auto text-white">
                <p className="text-red-400 font-bold mb-4">Упс! Запитуваний фільм не знайдено в базі даних.</p>
                <button type="button" onClick={() => navigate('/')} className="bg-white/5 border border-white/10 px-5 py-2.5 rounded-xl text-xs font-bold uppercase tracking-wider hover:bg-white/10 transition-colors border-none cursor-pointer text-white">Повернутися на афішу</button>
            </div>
        );
    }

    const getMoviePoster = (moviePath: MovieDto) => {
        const path = moviePath.effectivePosterUrl || moviePath.posterUrl;
        if (!path) return 'https://placehold.co/400x600?text=No+Poster';
        if (path.startsWith('/uploads')) return `http://localhost:5108${path}`; 
        return path;
    };

    return (
        <div className="w-full text-white select-none">
            <div className="relative w-full h-[25vh] md:h-[40vh] bg-black overflow-hidden border-b border-white/5">
                <div className="absolute inset-0 bg-cover bg-center opacity-25 scale-105 blur-[2px]" style={{ backgroundImage: `url(${movie.backdropUrl || getMoviePoster(movie)})` }}></div>
                <div className="absolute inset-0 z-20 bg-gradient-to-t from-dark-bg via-transparent to-transparent"></div>
                <div className="absolute top-6 left-6 z-30">
                    <button type="button" onClick={() => navigate('/')} className="bg-[#1a1c26]/90 backdrop-blur-md border border-white/5 px-4 py-2.5 rounded-xl text-xs font-bold hover:text-[#ffbd14] transition-all text-white cursor-pointer uppercase tracking-wider">
                        ← Назад до афіші
                    </button>
                </div>
            </div>

            <div className="max-w-7xl mx-auto px-6 -mt-20 relative z-30 pb-20">
                <div className="flex flex-col md:flex-row gap-8 md:gap-12 items-start">
                    <div className="w-48 md:w-56 mx-auto md:mx-0 flex-shrink-0 aspect-[2/3] rounded-2xl overflow-hidden border border-white/10 shadow-2xl bg-[#1a1c26]">
                        <img src={getMoviePoster(movie)} alt={movie.title} className="w-full h-full object-cover" />
                    </div>
                    
                    <div className="flex-grow pt-4 md:pt-20">
                        <div className="flex flex-wrap items-center gap-3 mb-3">
                            <span className="text-[#ffbd14] text-[10px] font-black uppercase tracking-widest bg-[#ffbd14]/10 px-2.5 py-1 rounded-md border border-[#ffbd14]/20">
                                {movie.genre?.split(',').join(' • ')}
                            </span>
                            {movie.ageRating && (
                                <span className="bg-white/5 px-2.5 py-1 rounded-md text-[10px] font-black border border-white/5 text-gray-300 uppercase tracking-wider">
                                    {movie.ageRating}
                                </span>
                            )}
                        </div>
                        <h1 className="text-3xl md:text-5xl font-black mb-4 tracking-tight">{movie.title}</h1>
                        <div className="flex flex-wrap gap-6 text-xs text-gray-400 mb-6 font-bold uppercase tracking-wider">
                            <div>🕒 {movie.durationInMinutes} хв.</div>
                            {movie.releaseDate && <div>📅 Прем'єра: {new Date(movie.releaseDate).toLocaleDateString('uk-UA')}</div>}
                        </div>
                        <p className="text-sm md:text-base text-gray-300 leading-relaxed max-w-3xl font-medium">{movie.description}</p>
                    </div>
                </div>

                <div className="grid grid-cols-1 lg:grid-cols-3 gap-8 mt-12 border-t border-white/5 pt-10">
                    <MovieTrailer videoId={getYouTubeEmbedUrl(movie.trailerUrl)} movieTitle={movie.title} />
                    <MovieSchedule sessions={movie.sessions ?? []} selectedCinemaId={selectedCinemaId} activeDay={activeDay} setActiveDay={setActiveDay} />
                </div>
            </div>
        </div>
    );
};