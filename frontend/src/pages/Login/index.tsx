import React, { useState } from 'react';
import { useAuthStore } from '@/store/authStore';
import { authApi } from '@/api/auth';

interface LoginProps {
  onSwitchToRegister: () => void;
}

export const LoginPage: React.FC<LoginProps> = ({ onSwitchToRegister }) => {
  const loginStore = useAuthStore((state) => state.login);
  
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [error, setError] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      const data = await authApi.login({ email, password });
      loginStore(data);
    } catch (err: any) {
      setError(err.response?.data?.message || 'Неправильний логін або пароль');
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="w-full max-w-md bg-dark-secondary border border-white/5 p-8 rounded-2xl shadow-2xl">
      <div className="text-center mb-6">
        <h2 className="text-2xl font-bold text-accent-gold">Вхід у StarPlex</h2>
        <p className="text-text-muted text-xs mt-1">Введіть свої дані для доступу до бронювання</p>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-500/10 border border-red-500/20 text-red-400 text-sm rounded-xl text-center">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        <div>
          <label className="block text-xs font-semibold text-text-muted mb-1.5 uppercase tracking-wider">Email адреса</label>
          <input
            type="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-accent-gold transition-colors"
            placeholder="user@example.com"
          />
        </div>

        <div>
          <label className="block text-xs font-semibold text-text-muted mb-1.5 uppercase tracking-wider">Пароль</label>
          <input
            type="password"
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-accent-gold transition-colors"
            placeholder="••••••••"
          />
        </div>

        <button
          type="submit"
          disabled={isLoading}
          className="w-full bg-accent-gold hover:bg-accent-gold/90 text-dark-bg font-bold py-3 rounded-xl transition-all shadow-lg disabled:opacity-50 disabled:cursor-not-allowed mt-2"
        >
          {isLoading ? 'Завантаження...' : 'Увійти'}
        </button>
      </form>

      <div className="mt-6 text-center text-sm text-text-muted">
        Не маєте акаунту?{' '}
        <button onClick={onSwitchToRegister} className="text-accent-gold hover:underline font-medium">
          Зареєструватися
        </button>
      </div>
    </div>
  );
};