import React, { useState } from 'react';
import { KeyRound } from 'lucide-react';
import { usersApi } from '@/api/users';
import { useToast } from '@/hooks/useToast';

export const ChangePasswordForm: React.FC = () => {
    const { showError, showSuccess } = useToast();
    const [oldPassword, setOldPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [isUpdating, setIsUpdating] = useState(false);

    const handleChangePassword = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!oldPassword || !newPassword) return;

        setIsUpdating(true);
        try {
            await usersApi.changePassword({ oldPassword, newPassword });
            showSuccess('Пароль успішно змінено!');
            setOldPassword('');
            setNewPassword('');
        } catch (err: any) {
            showError(err.response?.data?.message || err.response?.data?.Message || 'Помилка зміни пароля.');
        } finally {
            setIsUpdating(false);
        }
    };

    return (
        <div className="bg-dark-secondary border border-white/5 p-6 rounded-2xl flex flex-col gap-4">
            <h3 className="text-sm font-bold text-accent-gold flex items-center gap-1.5 uppercase tracking-wider">
                <KeyRound className="w-4 h-4" /> Безпека
            </h3>
            <form onSubmit={handleChangePassword} className="flex flex-col gap-3">
                <div>
                    <label className="block text-gray-400 font-semibold mb-1 uppercase text-[10px]">Поточний пароль</label>
                    <input
                        type="password"
                        value={oldPassword}
                        onChange={(e) => setOldPassword(e.target.value)}
                        className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-accent-gold text-xs"
                        required
                    />
                </div>
                <div>
                    <label className="block text-gray-400 font-semibold mb-1 uppercase text-[10px]">Новий пароль</label>
                    <input
                        type="password"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-accent-gold text-xs"
                        required
                    />
                </div>
                <button type="submit" disabled={isUpdating} className="bg-white/5 border border-white/10 hover:border-accent-gold text-white font-bold py-2 rounded-xl transition-all uppercase cursor-pointer text-[11px] disabled:opacity-50">
                    Оновити пароль
                </button>
            </form>
        </div>
    );
};