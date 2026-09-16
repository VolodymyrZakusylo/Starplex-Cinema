import React, { useEffect, useState, useRef } from 'react';
import { Html5QrcodeScanner } from 'html5-qrcode';
import { CheckCircle2, XCircle, ShieldCheck, ScanLine, Film, MapPin, Armchair, ArrowRight } from 'lucide-react';
import { bookingsApi } from '@/api/bookings';
import type { ScanResultDto } from '@/types/bookings';

export const CashierScannerPage: React.FC = () => {
    const [scanResult, setScanResult] = useState<ScanResultDto | null>(null);
    const [isProcessing, setIsProcessing] = useState<boolean>(false);

    const isProcessingRef = useRef<boolean>(false);
    const scanResultRef = useRef<ScanResultDto | null>(null);
    const containerRef = useRef<HTMLDivElement | null>(null);

    useEffect(() => {
        isProcessingRef.current = isProcessing;
    }, [isProcessing]);

    useEffect(() => {
        scanResultRef.current = scanResult;
    }, [scanResult]);

    const scannerRef = useRef<Html5QrcodeScanner | null>(null);

    useEffect(() => {
        let isCancelled = false;
        let isCleanedUp = false;

        const parentContainer = containerRef.current;
        if (!parentContainer) return;

        const hostId = `ticket-scanner-viewport-${Math.random().toString(36).substring(2, 9)}`;
        const hostEl = document.createElement('div');
        hostEl.id = hostId;
        hostEl.className = 'w-full rounded-2xl overflow-hidden bg-black';
        parentContainer.appendChild(hostEl);

        const scanner = new Html5QrcodeScanner(
            hostId,
            { 
                fps: 15, 
                qrbox: { width: 250, height: 250 },
                aspectRatio: 1.0
            },
            false
        );
        scannerRef.current = scanner;

        const onScanSuccess = async (decodedText: string) => {
            if (isCancelled || isProcessingRef.current || scanResultRef.current) return;
            
            isProcessingRef.current = true;
            setIsProcessing(true);
            try {
                const data = await bookingsApi.scanTicket(decodedText.trim());
                if (!isCancelled) {
                    scanResultRef.current = data;
                    setScanResult(data);
                }
            } catch (err: any) {
                if (!isCancelled) {
                    const errResult: ScanResultDto = {
                        isSuccess: false,
                        message: err.response?.data?.message || "Помилка сервера або недійсний код квитка."
                    };
                    scanResultRef.current = errResult;
                    setScanResult(errResult);
                }
            } finally {
                isProcessingRef.current = false;
                setIsProcessing(false);
            }
        };

        const onScanFailure = () => {};

        scanner.render(onScanSuccess, onScanFailure);

        return () => {
            if (isCleanedUp) return;
            isCleanedUp = true;
            isCancelled = true;

            if (scannerRef.current === scanner) {
                scannerRef.current = null;
            }

            const cleanUpHost = () => {
                if (hostEl.parentNode) {
                    hostEl.parentNode.removeChild(hostEl);
                }
            };

            scanner.clear()
                .catch(() => {})
                .finally(() => {
                    cleanUpHost();
                });
        };
    }, []);

    const handleClearAndNext = () => {
        scanResultRef.current = null;
        setScanResult(null);
    };

    return (
        <div className="w-full flex flex-col items-center justify-start gap-6 select-none">
            <div className="w-full max-w-md text-center border-b border-white/5 pb-4">
                <h1 className="text-2xl font-black tracking-tight flex items-center justify-center gap-2">
                    <ShieldCheck className="text-[#ffbd14] w-7 h-7" /> Контроль доступу
                </h1>
                <p className="text-gray-400 text-xs mt-1">Вхідний термінал авторизації електронних квитків StarPlex</p>
            </div>

            <div className="w-full max-w-md bg-[#1a1c26] border border-white/5 p-4 rounded-3xl shadow-2xl relative overflow-hidden">
                <div ref={containerRef} className="w-full"></div>
                
                {isProcessing && (
                    <div className="absolute inset-0 bg-black/75 backdrop-blur-sm flex items-center justify-center flex-col gap-2 rounded-3xl">
                        <div className="w-8 h-8 border-3 border-[#ffbd14] border-t-transparent rounded-full animate-spin"></div>
                        <span className="text-xs font-bold text-[#ffbd14] tracking-wide">Перевірка квитка в базі...</span>
                    </div>
                )}

                {scanResult && (
                    <div className="absolute inset-0 bg-black/40 backdrop-blur-md flex items-center justify-center text-center p-6 rounded-3xl">
                        <span className="text-xs font-bold text-gray-400 max-w-[200px]">Сканер призупинено. Обробіть поточний квиток нижче.</span>
                    </div>
                )}
            </div>

            {scanResult && (
                <div className={`w-full max-w-md p-5 rounded-2xl border flex flex-col gap-4 shadow-xl animate-fadeIn ${
                    scanResult.isSuccess ? 'bg-emerald-500/10 border-emerald-500/30' : 'bg-red-500/10 border-red-500/30'
                }`}>
                    <div className="flex items-start gap-3.5">
                        {scanResult.isSuccess ? (
                            <CheckCircle2 className="w-9 h-9 text-emerald-400 shrink-0 mt-0.5" />
                        ) : (
                            <XCircle className="w-9 h-9 text-red-400 shrink-0 mt-0.5" />
                        )}
                        <div>
                            <h3 className={`text-base font-black uppercase tracking-wider ${scanResult.isSuccess ? 'text-emerald-400' : 'text-red-400'}`}>
                                {scanResult.isSuccess ? "Вхід Дозволено" : "Доступ Заборонено"}
                            </h3>
                            <p className="text-xs text-gray-300 font-medium mt-1 leading-relaxed">{scanResult.message}</p>
                        </div>
                    </div>

                    {scanResult.isSuccess && (
                        <div className="flex flex-col gap-2.5 border-t border-white/5 pt-3.5 text-xs text-gray-400 font-medium">
                            <div className="flex items-center gap-2.5">
                                <Film className="w-4 h-4 text-[#ffbd14]" />
                                <span>Фільм: <strong className="text-white font-semibold">{scanResult.movieTitle}</strong></span>
                            </div>
                            <div className="flex items-center gap-2.5">
                                <MapPin className="w-4 h-4 text-[#ffbd14]" />
                                <span>Локація: <strong className="text-white font-semibold">{scanResult.hallName}{scanResult.startTime ? ` (Початок о ${scanResult.startTime})` : ''}</strong></span>
                            </div>
                            <div className="flex items-center gap-2.5">
                                <Armchair className="w-4 h-4 text-[#ffbd14]" />
                                <span>Посадочне місце: <strong className="text-emerald-400 font-black font-mono">Ряд {scanResult.row}, Місце {scanResult.number}</strong></span>
                            </div>
                        </div>
                    )}

                    <button
                        type="button"
                        onClick={handleClearAndNext}
                        className={`w-full py-3 rounded-xl text-xs font-black uppercase tracking-wider border-none cursor-pointer transition-all flex items-center justify-center gap-2 shadow-lg active:scale-[0.99] ${
                            scanResult.isSuccess 
                                ? 'bg-emerald-500 hover:bg-emerald-600 text-black shadow-emerald-500/10' 
                                : 'bg-red-500 hover:bg-red-600 text-white shadow-red-500/10'
                        }`}
                    >
                        <span>{scanResult.isSuccess ? "Пропустити гостя" : "Зрозуміло, наступний"}</span>
                        <ArrowRight className="w-4 h-4 stroke-[2.5]" />
                    </button>
                </div>
            )}

            {!scanResult && !isProcessing && (
                <div className="text-center text-gray-500 text-xs flex items-center gap-2 animate-pulse mt-1 font-semibold uppercase tracking-wider">
                    <ScanLine className="w-4 h-4 text-gray-500" />
                    <span>Камера активна. Очікування квитка...</span>
                </div>
            )}
        </div>
    );
};