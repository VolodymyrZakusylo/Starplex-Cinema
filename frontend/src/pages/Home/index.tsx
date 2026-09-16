import React, { useEffect, useState } from 'react';
import { moviesApi } from '@/api/movies';
import { cinemasApi } from '@/api/cinemas';
import { useCinemaStore } from '@/store/cinemaStore';
import { useNavigate } from 'react-router-dom';
import { Search, SlidersHorizontal, Clock, Star, MapPin } from 'lucide-react';
import { useToast } from '@/hooks/useToast';
import type { Movie, CinemaDto } from '@/types';
import { getMediaUrl } from '@/utils/media';

export const HomePage: React.FC = () => {
  const navigate = useNavigate();
  const { showError } = useToast();
  const { selectedCinemaId } = useCinemaStore();
  const [currentCinema, setCurrentCinema] = useState<CinemaDto | null>(null);

  const [movies, setMovies] = useState<Movie[]>([]);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [selectedStatus, setSelectedStatus] = useState<'All' | 'NowShowing' | 'ComingSoon'>('All');

  const [searchQuery, setSearchQuery] = useState<string>('');
  const [selectedGenre, setSelectedGenre] = useState<string>('All');
  const [genres, setGenres] = useState<string[]>([]);

  useEffect(() => {
    if (!selectedCinemaId) return;
    cinemasApi.getById(selectedCinemaId)
      .then(data => setCurrentCinema(data))
      .catch(() => setCurrentCinema(null));
  }, [selectedCinemaId]);

  useEffect(() => {
    const fetchMovies = async () => {
      setIsLoading(true);
      try {
        const statusParam = selectedStatus === 'All' ? undefined : selectedStatus;
        const data = await moviesApi.getAll(statusParam, selectedCinemaId);
        setMovies(data);

        const allGenres = new Set<string>();
        data.forEach((movie) => {
          if (movie.genre) {
            movie.genre.split(',').forEach((g) => {
              const trimmed = g.trim();
              if (trimmed) allGenres.add(trimmed);
            });
          }
        });
        setGenres(Array.from(allGenres).sort());
      } catch (err: any) {
        showError('Не вдалося завантажити афішу кінотеатру. Перевірте підключення.');
      } finally {
        setIsLoading(false);
      }
    };

    fetchMovies();
  }, [selectedStatus, selectedCinemaId, showError]);

  useEffect(() => {
    setSelectedGenre('All');
  }, [selectedStatus]);

  const filteredMovies = movies.filter((movie) => {
    const matchesSearch = movie.title.toLowerCase().includes(searchQuery.toLowerCase());
    const matchesGenre = selectedGenre === 'All' ||
      (movie.genre && movie.genre.toLowerCase().includes(selectedGenre.toLowerCase()));

    return matchesSearch && matchesGenre;
  });

  const getMoviePoster = (movie: Movie) => {
    const path = movie.effectivePosterUrl || movie.posterUrl;
    return getMediaUrl(path);
  };

  return (
    <div className="w-full max-w-7xl mx-auto px-4 py-8 select-none">

      <div className="flex flex-col lg:flex-row lg:items-center lg:justify-between border-b border-white/5 pb-6 mb-8 gap-4">
        <div>
          <h1 className="text-3xl font-black text-white tracking-tight">Зараз у кіно</h1>
          <p className="text-gray-400 text-sm mt-1 flex items-center gap-1.5">
            <MapPin className="w-3.5 h-3.5 text-[#ffbd14]" />
            Актуальні сеанси та прем'єри у {currentCinema ? `${currentCinema.city} — ${currentCinema.name}` : 'StarPlex'}
          </p>
        </div>

        <div className="flex bg-dark-secondary p-1 rounded-xl border border-white/5 self-start">
          {(['All', 'NowShowing', 'ComingSoon'] as const).map((status) => (
            <button
              key={status}
              type="button"
              onClick={() => setSelectedStatus(status)}
              className={`px-4 py-2 text-sm font-bold rounded-lg transition-all border-none cursor-pointer ${selectedStatus === status
                  ? 'bg-[#ffbd14] text-black shadow-md shadow-[#ffbd14]/10'
                  : 'text-gray-400 hover:text-white bg-transparent'
                }`}
            >
              {status === 'All' && 'Вся афіша'}
              {status === 'NowShowing' && 'Зараз у прокаті'}
              {status === 'ComingSoon' && 'Скоро у кіно'}
            </button>
          ))}
        </div>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-3 gap-4 mb-8 bg-dark-secondary p-4 rounded-2xl border border-white/5 shadow-md">
        <div className="relative col-span-1 sm:col-span-2">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder="Пошук фільму за назвою..."
            className="w-full bg-dark-bg border border-white/10 rounded-xl pl-10 pr-4 py-3 text-sm text-white placeholder-gray-500 focus:outline-none focus:border-[#ffbd14] transition-colors"
          />
          <Search className="w-4 h-4 text-gray-500 absolute left-3.5 top-3.5" />
        </div>

        <div className="relative">
          <select
            value={selectedGenre}
            onChange={(e) => setSelectedGenre(e.target.value)}
            className="w-full bg-dark-bg border border-white/10 rounded-xl pl-10 pr-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14] transition-colors appearance-none cursor-pointer font-semibold"
          >
            <option value="All">Всі жанри</option>
            {genres.map((genre) => (
              <option key={genre} value={genre}>{genre}</option>
            ))}
          </select>
          <SlidersHorizontal className="w-4 h-4 text-gray-500 absolute left-3.5 top-3.5 pointer-events-none" />
          <div className="absolute inset-y-0 right-0 flex items-center pr-3 pointer-events-none text-gray-500 text-xs font-black">▼</div>
        </div>
      </div>

      {isLoading && (
        <div className="flex flex-col items-center justify-center py-32 space-y-4">
          <div className="w-10 h-10 border-4 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
          <p className="text-gray-400 text-xs font-bold uppercase tracking-wider">Завантаження фільмів та постерів...</p>
        </div>
      )}

      {!isLoading && filteredMovies.length === 0 && (
        <div className="text-center py-20 bg-dark-secondary rounded-2xl border border-white/5 p-6 shadow-inner flex flex-col items-center justify-center gap-4">
          <p className="text-gray-400 text-sm">Нічого не знайдено за вказаними критеріями фільтрації.</p>
          <button
            type="button"
            onClick={() => {
              setSearchQuery('');
              setSelectedGenre('All');
            }}
            className="bg-white/5 hover:bg-white/10 border border-white/10 px-4 py-2 rounded-xl text-xs font-bold uppercase tracking-wider text-white transition-all border-none cursor-pointer"
          >
            Скинути фільтри
          </button>
        </div>
      )}

      {!isLoading && filteredMovies.length > 0 && (
        <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 lg:grid-cols-4 gap-6">
          {filteredMovies.map((movie) => (
            <div
              key={movie.id}
              onClick={() => {
                const movieParam = movie.slug ? `${movie.slug}--${movie.id}` : movie.id;
                navigate(`/movies/${movieParam}`);
              }}
              className="group bg-dark-secondary border border-white/5 rounded-2xl overflow-hidden hover:border-[#ffbd14]/20 hover:shadow-2xl hover:shadow-[#ffbd14]/5 transition-all duration-300 flex flex-col cursor-pointer"
            >
              <div className="relative aspect-[2/3] w-full overflow-hidden bg-dark-bg border-b border-white/5">
                <img
                  src={getMoviePoster(movie)}
                  alt={movie.title}
                  className="w-full h-full object-cover group-hover:scale-105 transition-transform duration-500"
                  loading="lazy"
                  onError={(e) => { (e.target as HTMLImageElement).src = 'https://placehold.co/400x600?text=No+Poster'; }}
                />
                {movie.tmdbRating !== undefined && movie.tmdbRating > 0 && (
                  <div className="absolute top-3 left-3 bg-dark-bg/80 backdrop-blur-md border border-white/10 px-2.5 py-1 rounded-lg flex items-center space-x-1 shadow-lg">
                    <Star className="text-[#ffbd14] w-3 h-3 fill-[#ffbd14]" />
                    <span className="text-white text-xs font-black">{movie.tmdbRating.toFixed(1)}</span>
                  </div>
                )}
                {movie.ageRating && (
                  <div className="absolute top-3 right-3 bg-black/60 backdrop-blur-sm px-2 py-0.5 rounded text-[10px] font-bold text-white uppercase tracking-wider border border-white/5">
                    {movie.ageRating}
                  </div>
                )}
              </div>

              <div className="p-4 flex flex-col flex-grow">
                <span className="text-[#ffbd14] text-[10px] font-black uppercase tracking-widest mb-1.5 line-clamp-1">
                  {movie.genre?.split(',').join(' • ')}
                </span>
                <h3 className="text-base font-black text-white group-hover:text-[#ffbd14] transition-colors line-clamp-1 tracking-tight">
                  {movie.title}
                </h3>
                <p className="text-gray-400 text-xs mt-1.5 line-clamp-2 flex-grow leading-relaxed font-medium">
                  {movie.description}
                </p>

                <div className="mt-5 pt-3 border-t border-white/5 flex items-center justify-between text-xs text-gray-400 font-bold uppercase tracking-wider">
                  <div className="flex items-center space-x-1">
                    <Clock className="w-3.5 h-3.5 text-[#ffbd14]" />
                    <span>{movie.durationInMinutes} хв.</span>
                  </div>
                  <span className="text-[#ffbd14] group-hover:translate-x-1 transition-transform font-black text-sm leading-none">
                    →
                  </span>
                </div>
              </div>
            </div>
          ))}
        </div>
      )}
    </div>
  );
};