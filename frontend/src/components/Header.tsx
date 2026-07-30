import React, { useEffect, useState } from 'react';
import { Link, useLocation } from 'react-router-dom';
import { useAuthStore } from '@/store/authStore';
import { useCinemaStore } from '@/store/cinemaStore';
import { MapPin, ShieldAlert } from 'lucide-react';
import api from '@/api/axios';

interface CinemaDto {
  id: string;
  name: string;
  city: string;
}

export const Header: React.FC = () => {
  const { isAuthenticated, user, logout } = useAuthStore();
  const { selectedCinemaId, setCinemaId } = useCinemaStore();
  const location = useLocation();

  const [cinemas, setCinemas] = useState<CinemaDto[]>([]);
  const isAuthPage = location.pathname === '/login' || location.pathname === '/register';
  const isAdminOrManager = user?.roles.some(role => ['SuperAdmin', 'CinemaManager'].includes(role));

  useEffect(() => {
    const fetchCinemas = async () => {
      try {
        const response = await api.get<CinemaDto[]>('/Cinemas');
        setCinemas(response.data);
        
        if (response.data.length > 0 && !selectedCinemaId) {
          setCinemaId(response.data[0].id);
        }
      } catch (err) {
        console.error('Не вдалося завантажити філії кінотеатрів:', err);
      }
    };
    fetchCinemas();
  }, [selectedCinemaId, setCinemaId]);

  return (
    <header className="bg-dark-secondary border-b border-white/5 px-6 py-4 sticky top-0 z-50 backdrop-blur-md bg-dark-secondary/90">
      <div className="max-w-7xl mx-auto flex items-center justify-between gap-4">
        
        <div className="flex items-center space-x-6">
          <Link to="/" className="flex items-center space-x-2 cursor-pointer group flex-shrink-0">
            <span className="text-accent-gold text-2xl group-hover:scale-110 transition-transform">★</span>
            <span className="font-bold tracking-wider text-lg">STARPLEX</span>
          </Link>

          {!isAuthPage && cinemas.length > 0 && (
            <div className="relative flex items-center bg-dark-bg/50 border border-white/10 rounded-xl px-3 py-1.5 hover:border-accent-gold/50 transition-colors">
              <MapPin className="w-3.5 h-3.5 text-accent-gold mr-2 flex-shrink-0" />
              <select
                value={selectedCinemaId}
                onChange={(e) => setCinemaId(e.target.value)}
                className="bg-transparent text-xs font-semibold text-white focus:outline-none pr-6 appearance-none cursor-pointer"
              >
                {cinemas.map((c) => (
                  <option key={c.id} value={c.id} className="bg-dark-secondary text-white">
                    {c.city} — {c.name}
                  </option>
                ))}
              </select>
              <div className="absolute inset-y-0 right-2.5 flex items-center pointer-events-none text-gray-500 text-[10px]">▼</div>
            </div>
          )}
        </div>

        <div className="flex items-center space-x-4 flex-shrink-0">
          {isAuthenticated ? (
            <>
              {isAdminOrManager && (
                <Link
                  to="/admin/movies"
                  className="bg-accent-gold/10 hover:bg-accent-gold/20 text-accent-gold text-xs font-bold px-4 py-2 rounded-xl border border-accent-gold/20 transition-all flex items-center gap-1.5"
                >
                  <ShieldAlert className="w-3.5 h-3.5" /> Панель управління
                </Link>
              )}

              <span className="text-xs text-text-muted hidden md:inline">
                Вітаємо, <span className="text-white font-medium">{user?.firstName || 'User'}</span>
              </span>

              <Link
                to="/profile"
                className="bg-white/5 hover:bg-white/10 text-xs font-semibold px-4 py-2 rounded-xl border border-white/10 text-white transition-colors"
              >
                👤 Профіль
              </Link>

              <button
                onClick={logout}
                className="bg-red-500/10 hover:bg-red-500/20 text-red-400 text-xs font-semibold px-4 py-2 rounded-xl border border-red-500/20 transition-colors"
              >
                Вийти
              </button>
            </>
          ) : (
            !isAuthPage && (
              <Link
                to="/login"
                className="bg-accent-gold hover:bg-accent-gold/90 text-dark-bg text-xs font-bold px-5 py-2.5 rounded-xl transition-all shadow-md"
              >
                Увійти
              </Link>
            )
          )}
        </div>
      </div>
    </header>
  );
};