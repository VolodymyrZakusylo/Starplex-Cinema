import React, { useState, createContext, useContext, useCallback } from 'react';
import { AlertCircle, CheckCircle2, AlertTriangle } from 'lucide-react';

interface ToastContextType {
    showError: (message: string) => void;
    showSuccess: (message: string) => void;
    confirm: (message: string, onConfirm: () => void) => void;
}

const ToastContext = createContext<ToastContextType | undefined>(undefined);

export const ToastProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
    const [toast, setToast] = useState<{ message: string; type: 'success' | 'error' } | null>(null);
    const [dialog, setDialog] = useState<{ message: string; onConfirm: () => void } | null>(null);

    const showError = useCallback((message: string) => {
        setToast({ message, type: 'error' });
        setTimeout(() => setToast(null), 3500);
    }, []);

    const showSuccess = useCallback((message: string) => {
        setToast({ message, type: 'success' });
        setTimeout(() => setToast(null), 3500);
    }, []);

    const confirm = useCallback((message: string, onConfirm: () => void) => {
        setDialog({ message, onConfirm });
    }, []);

    return (
        <ToastContext.Provider value={{ showError, showSuccess, confirm }}>
            {children}
            
            {toast && (
                <div className={`fixed bottom-5 right-5 z-[9999] flex items-center gap-3 px-5 py-3.5 rounded-2xl border shadow-2xl backdrop-blur-md text-xs font-bold uppercase tracking-wider animate-in fade-in slide-in-from-right-4 duration-300 ${
                    toast.type === 'error' 
                        ? 'bg-red-500/10 border-red-500/30 text-red-400' 
                        : 'bg-emerald-500/10 border-emerald-500/30 text-emerald-400'
                }`}>
                    {toast.type === 'error' ? <AlertCircle className="w-4 h-4" /> : <CheckCircle2 className="w-4 h-4" />}
                    <span>{toast.message}</span>
                </div>
            )}

            {dialog && (
                <div className="fixed inset-0 z-[9999] flex items-center justify-center bg-black/60 backdrop-blur-sm p-4 animate-in fade-in duration-200">
                    <div className="bg-[#1a1c26] border border-white/10 p-6 rounded-2xl shadow-2xl max-w-sm w-full animate-in zoom-in-95 duration-200">
                        <AlertTriangle className="w-10 h-10 text-[#ffbd14] mb-4" />
                        <h3 className="text-white font-black text-lg mb-2">Підтвердження</h3>
                        <p className="text-gray-400 text-sm mb-6">{dialog.message}</p>
                        <div className="flex gap-3">
                            <button 
                                onClick={() => setDialog(null)} 
                                className="flex-1 px-4 py-2 bg-white/5 hover:bg-white/10 rounded-xl font-bold text-xs uppercase text-white transition-all cursor-pointer border-none"
                            >
                                Скасувати
                            </button>
                            <button
                                onClick={() => {
                                    const handleConfirm = dialog.onConfirm;
                                    setDialog(null);
                                    handleConfirm();
                                }}
                                className="flex-1 px-4 py-2 bg-red-500 hover:bg-red-600 rounded-xl font-bold text-xs uppercase text-white transition-all cursor-pointer border-none"
                            >
                                Підтвердити
                            </button>
                        </div>
                    </div>
                </div>
            )}
        </ToastContext.Provider>
    );
};

export const useToast = () => {
    const context = useContext(ToastContext);
    if (!context) throw new Error('useToast must be used within a ToastProvider');
    return context;
};