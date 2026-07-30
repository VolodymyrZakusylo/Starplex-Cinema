import React, { useState } from 'react';
import { User } from 'lucide-react';
import api from '@/api/axios';

interface ProfileDataFormProps {
    initialFirstName: string;
    initialLastName: string;
}

export const ProfileDataForm: React.FC<ProfileDataFormProps> = ({ initialFirstName, initialLastName }) => {
    const [firstName, setFirstName] = useState(initialFirstName);
    const [lastName, setLastName] = useState(initialLastName);
    const [isUpdating, setIsUpdating] = useState(false);

    const handleUpdateProfile = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!firstName.trim() || !lastName.trim()) return;

        setIsUpdating(true);
        try {
            await api.put('/User/update-profile', { 
                firstName: firstName.trim(), 
                lastName: lastName.trim() 
            });
            
            const storedUser = localStorage.getItem('user');
            if (storedUser) {
                const parsed = JSON.parse(storedUser);
                parsed.firstName = firstName.trim();
                parsed.lastName = lastName.trim();
                localStorage.setItem('user', JSON.stringify(parsed));
            }
            alert('🎉 Особисті дані успішно оновлено!');
        } catch (err) {
            alert('Не вдалося оновити особисті дані.');
        } finally {
            setIsUpdating(false);
        }
    };

    return (
        <div className="bg-dark-secondary border border-white/5 p-6 rounded-2xl flex flex-col gap-4">
            <h3 className="text-sm font-bold text-accent-gold flex items-center gap-1.5 uppercase tracking-wider">
                <User className="w-4 h-4" /> Особисті дані
            </h3>
            <form onSubmit={handleUpdateProfile} className="flex flex-col gap-4">
                <div className="grid grid-cols-1 sm:grid-cols-2 gap-3">
                    <div>
                        <label className="block text-gray-400 font-semibold mb-1 uppercase text-[10px]">Ім'я</label>
                        <input
                            type="text"
                            value={firstName}
                            onChange={(e) => setFirstName(e.target.value)}
                            className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-accent-gold text-xs"
                            required
                        />
                    </div>
                    <div>
                        <label className="block text-gray-400 font-semibold mb-1 uppercase text-[10px]">Прізвище</label>
                        <input
                            type="text"
                            value={lastName}
                            onChange={(e) => setLastName(e.target.value)}
                            className="w-full bg-dark-bg border border-white/10 rounded-xl px-4 py-2.5 text-white focus:outline-none focus:border-accent-gold text-xs"
                            required
                        />
                    </div>
                </div>
                <button type="submit" disabled={isUpdating} className="bg-white/5 border border-white/10 hover:border-accent-gold hover:bg-accent-gold/10 text-white font-bold py-2.5 rounded-xl transition-all uppercase cursor-pointer tracking-wider text-[11px] disabled:opacity-50">
                    Зберегти зміни
                </button>
            </form>
        </div>
    );
};