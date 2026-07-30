import React, { useState } from 'react';
import { authApi } from '@/api/auth';

interface RegisterProps {
  onSwitchToLogin: () => void;
}

export const RegisterPage: React.FC<RegisterProps> = ({ onSwitchToLogin }) => {
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [firstName, setFirstName] = useState('');
  const [lastName, setLastName] = useState('');
  const [dateOfBirth, setDateOfBirth] = useState('');
  
  const [error, setError] = useState<string | null>(null);
  const [isSuccess, setIsSuccess] = useState(false);
  const [isLoading, setIsLoading] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError(null);
    setIsLoading(true);

    try {
      await authApi.register({ email, password, firstName, lastName, dateOfBirth });
      setIsSuccess(true);
    } catch (err: any) {
      const serverError = err.response?.data?.Errors?.[0] || err.response?.data?.Message;
      setError(serverError || 'Помилка при реєстрації. Спробуйте ще раз.');
    } finally {
      setIsLoading(false);
    }
  };

  if (isSuccess) {
    return (
      <div className="w-full max-w-md bg-dark-secondary border border-white/5 p-8 rounded-2xl text-center shadow-2xl">
        <div className="text-4xl mb-3">🎉</div>
        <h2 className="text-2xl font-bold text-accent-gold mb-2">Успішна реєстрація!</h2>
        <p className="text-text-muted text-sm mb-6">Акаунт створено. Тепер ви можете увійти у свій профіль.</p>
        <button
          onClick={onSwitchToLogin}
          className="w-full bg-accent-gold hover:bg-accent-gold/90 text-dark-bg font-bold py-3 rounded-xl transition-colors"
        >
          Перейти до входу
        </button>
      </div>
    );
  }

  return (
    <div className="w-full max-w-md bg-dark-secondary border border-white/5 p-8 rounded-2xl shadow-2xl">
      <div className="text-center mb-6">
        <h2 className="text-2xl font-bold text-accent-gold">Реєстрація акаунту</h2>
        <p className="text-text-muted text-xs mt-1">Створіть свій профіль мережі кінотеатрів</p>
      </div>

      {error && (
        <div className="mb-4 p-3 bg-red-500/10 border border-red-500/20 text-red-400 text-sm rounded-xl text-center">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit} className="space-y-4">
        {/* Рядок: Ім'я та Прізвище */}
        <div className="grid grid-cols-2 gap-4">
          <div>
            <label className="block text-xs font-semibold text-text-muted mb-1.5 uppercase tracking-wider">Ім'я</label>
            <input
              type="text"
              required
              value={firstName}
              onChange={(e) => setFirstName(e.target.value)}
              className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-accent-gold transition-colors"
              placeholder="Ім`я"
            />
          </div>
          <div>
            <label className="block text-xs font-semibold text-text-muted mb-1.5 uppercase tracking-wider">Прізвище</label>
            <input
              type="text"
              required
              value={lastName}
              onChange={(e) => setLastName(e.target.value)}
              className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-accent-gold transition-colors"
              placeholder="Прізвище"
            />
          </div>
        </div>

        {/* 🔥 ДОДАНО: Поле вибору дати народження */}
        <div>
          <label className="block text-xs font-semibold text-text-muted mb-1.5 uppercase tracking-wider">Дата народження</label>
          <input
            type="date"
            required
            value={dateOfBirth}
            onChange={(e) => setDateOfBirth(e.target.value)}
            className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-accent-gold transition-colors text-left"
          />
        </div>

        {/* Поле: Email */}
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

        {/* Поле: Пароль */}
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
          {isLoading ? 'Реєстрація...' : 'Створити акаунт'}
        </button>
      </form>

      <div className="mt-6 text-center text-sm text-text-muted">
        Вже є профіль?{' '}
        <button onClick={onSwitchToLogin} className="text-accent-gold hover:underline font-medium">
          Увійти
        </button>
      </div>
    </div>
  );
};