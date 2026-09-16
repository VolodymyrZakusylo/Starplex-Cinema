import React, { useState, useEffect } from 'react';
import type { HallDto } from '@/types';

export interface HallFormPayload {
    id?: string;
    cinemaId: string;
    name: string;
    totalRows: number;
    seatsPerRow: number;
    isActive: boolean;
}

interface HallFormModalProps {
    isOpen: boolean;
    editingHall: HallDto | null;
    selectedCinemaId: string;
    onClose: () => void;
    onSave: (payload: HallFormPayload) => Promise<void>;
}

export const HallFormModal: React.FC<HallFormModalProps> = ({
    isOpen,
    editingHall,
    selectedCinemaId,
    onClose,
    onSave
}) => {
    const [hallName, setHallName] = useState<string>('');
    const [totalRows, setTotalRows] = useState<number>(8);
    const [seatsPerRow, setSeatsPerRow] = useState<number>(10);
    const [isActive, setIsActive] = useState<boolean>(true);
    const [errorMessage, setErrorMessage] = useState<string>('');
    const [isSubmitting, setIsSubmitting] = useState<boolean>(false);

    useEffect(() => {
        if (!isOpen) return;

        if (editingHall) {
            setHallName(editingHall.name);
            setTotalRows(editingHall.totalRows);
            setSeatsPerRow(editingHall.seatsPerRow);
            setIsActive(editingHall.isActive);
        } else {
            setHallName('');
            setTotalRows(8);
            setSeatsPerRow(10);
            setIsActive(true);
        }
        setErrorMessage('');
    }, [isOpen, editingHall]);

    if (!isOpen) return null;

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        setErrorMessage('');

        if (!hallName.trim()) {
            setErrorMessage('Назва залу не може бути порожньою');
            return;
        }

        const payload: HallFormPayload = {
            id: editingHall?.id || undefined,
            cinemaId: selectedCinemaId,
            name: hallName.trim(),
            totalRows: Number(totalRows),
            seatsPerRow: Number(seatsPerRow),
            isActive
        };

        setIsSubmitting(true);
        try {
            await onSave(payload);
            onClose();
        } catch (err: any) {
            setErrorMessage(err.response?.data?.message || 'Сталася помилка при збереженні залу.');
        } finally {
            setIsSubmitting(false);
        }
    };

    return (
        <div className="fixed inset-0 z-50 flex items-center justify-center p-4 bg-black/70 backdrop-blur-sm">
            <div className="w-full max-w-md bg-[#1a1c26] border border-white/10 rounded-2xl p-6 relative">
                <h2 className="text-xl font-black tracking-tight mb-4 text-white">
                    {editingHall ? 'Редагувати зал' : 'Створити новий зал'}
                </h2>
                {errorMessage && (
                    <p className="text-xs text-red-400 mb-3 bg-red-500/10 border border-red-500/10 p-2 rounded-lg font-bold">{errorMessage}</p>
                )}
                <form onSubmit={handleSubmit} className="space-y-4">
                    <div>
                        <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-1">Назва залу</label>
                        <input
                            type="text"
                            value={hallName}
                            onChange={(e) => setHallName(e.target.value)}
                            placeholder="Наприклад: Синій зал, IMAX"
                            className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white focus:outline-none focus:border-[#ffbd14]"
                        />
                    </div>
                    <div className="grid grid-cols-2 gap-4">
                        <div>
                            <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-1">Всього рядів</label>
                            <input
                                type="number"
                                value={totalRows}
                                onChange={(e) => setTotalRows(Number(e.target.value))}
                                disabled={!!editingHall}
                                className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed"
                            />
                        </div>
                        <div>
                            <label className="block text-xs font-bold text-gray-400 uppercase tracking-wider mb-1">Місць у ряді</label>
                            <input
                                type="number"
                                value={seatsPerRow}
                                onChange={(e) => setSeatsPerRow(Number(e.target.value))}
                                disabled={!!editingHall}
                                className="w-full bg-[#111219] border border-white/10 rounded-xl px-4 py-3 text-sm text-white disabled:opacity-50 disabled:cursor-not-allowed"
                            />
                        </div>
                    </div>
                    <div className="flex items-center justify-end gap-3 pt-4 border-t border-white/5">
                        <button type="button" onClick={onClose} className="bg-white/5 text-white font-bold text-xs px-4 py-3 rounded-xl border-none cursor-pointer">Скасувати</button>
                        <button type="submit" disabled={isSubmitting} className="bg-[#ffbd14] text-black font-black text-xs px-5 py-3 rounded-xl uppercase tracking-wider border-none cursor-pointer disabled:opacity-50">Зберегти</button>
                    </div>
                </form>
            </div>
        </div>
    );
};
