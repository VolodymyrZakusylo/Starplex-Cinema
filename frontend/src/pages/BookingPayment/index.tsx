import React, { useState } from 'react';
import { useLocation, useNavigate } from 'react-router-dom';
import { loadStripe } from '@stripe/stripe-js';
import { Elements, CardElement, useStripe, useElements } from '@stripe/react-stripe-js';
import { Ticket, AlertTriangle, ShieldCheck } from 'lucide-react';
import { usePageTitle } from '@/hooks/usePageTitle';
import { bookingsApi } from '@/api/bookings';

const stripePromise = loadStripe(import.meta.env.VITE_STRIPE_PUBLIC_KEY);

interface CheckoutFormProps {
    bookingId: string;
    totalAmount: number;
    clientSecret: string;
}

const CheckoutForm: React.FC<CheckoutFormProps> = ({ bookingId, totalAmount, clientSecret }) => {
    const stripe = useStripe();
    const elements = useElements();
    const navigate = useNavigate();
    
    const [isProcessing, setIsProcessing] = useState<boolean>(false);
    const [errorMessage, setErrorMessage] = useState<string | null>(null);

    const handleSubmit = async (event: React.FormEvent) => {
        event.preventDefault();

        if (!stripe || !elements) return;

        setIsProcessing(true);
        setErrorMessage(null);

        const cardElement = elements.getElement(CardElement);
        if (!cardElement) return;

        try {
            const { error, paymentIntent } = await stripe.confirmCardPayment(clientSecret, {
                payment_method: {
                    card: cardElement,
                },
            });

            if (error) {
                setErrorMessage(error.message || 'Сталася помилка під час обробки транзакції.');
                setIsProcessing(false);
            } else if (paymentIntent && paymentIntent.status === 'succeeded') {
                try {
                    await bookingsApi.confirm(bookingId);
                    navigate('/booking/success', { state: { bookingId } });
                } catch (backendError: any) {
                    console.error('Помилка підтвердження на бекенді:', backendError);
                    setErrorMessage(
                        backendError.response?.data?.Message || 
                        'Гроші успішно списано, але стався збій при генерації електронних квитків. Будь ласка, зверніться до підтримки кінотеатру.'
                    );
                    setIsProcessing(false);
                }
            }
        } catch (err) {
            setErrorMessage('Не вдалося встановити захищене з’єднання з платіжним шлюзом Stripe.');
            setIsProcessing(false);
        }
    };

    return (
        <form onSubmit={handleSubmit} className="space-y-6 text-xs">
            <div className="bg-[#111219] border border-white/10 rounded-xl p-4">
                <label className="block text-[10px] font-bold text-gray-400 mb-3 uppercase tracking-wider">
                    Дані банківської картки
                </label>
                <CardElement
                    options={{
                        style: {
                            base: {
                                color: '#ffffff',
                                fontFamily: 'Inter, sans-serif',
                                fontSmoothing: 'antialiased',
                                fontSize: '14px',
                                '::placeholder': {
                                    color: '#555866',
                                },
                            },
                            invalid: {
                                color: '#f87171',
                                iconColor: '#f87171',
                            },
                        },
                    }}
                />
            </div>

            {errorMessage && (
                <div className="p-3 bg-red-500/10 border border-red-500/20 text-red-400 text-xs font-medium rounded-xl flex items-center justify-center gap-2 animate-fadeIn">
                    <AlertTriangle className="w-4 h-4 shrink-0" />
                    <div className="leading-relaxed">{errorMessage}</div>
                </div>
            )}

            <button
                type="submit"
                disabled={!stripe || isProcessing}
                className="w-full bg-[#ffbd14] hover:bg-[#e0a40f] disabled:bg-white/5 text-black disabled:text-gray-500 font-black py-3.5 rounded-xl transition-all shadow-lg text-xs uppercase tracking-widest disabled:cursor-not-allowed flex items-center justify-center gap-2 border-none cursor-pointer"
            >
                {isProcessing ? (
                    <>
                        <div className="w-4 h-4 border-2 border-black border-t-transparent rounded-full animate-spin"></div>
                        <span>Обробка платежу...</span>
                    </>
                ) : (
                    <span>Оплатити {totalAmount} ₴</span>
                )}
            </button>
        </form>
    );
};

export const BookingPaymentPage: React.FC = () => {
    usePageTitle('Оплата замовлення');
    const location = useLocation();
    const navigate = useNavigate();

    const { bookingId, totalAmount, clientSecret } = (location.state as {
        bookingId: string;
        totalAmount: number;
        clientSecret: string;
    }) || {};

    if (!bookingId || !clientSecret || !totalAmount) {
        return (
            <div className="text-center py-40 text-white flex flex-col items-center justify-center gap-4">
                <p className="text-red-400 font-bold">Критична помилка: Сесійні дані платежу відсутні.</p>
                <button type="button" onClick={() => navigate(-1)} className="bg-white/5 border border-white/10 px-5 py-2.5 rounded-xl text-xs font-bold uppercase tracking-wider text-white border-none cursor-pointer hover:bg-white/10 transition-all">
                    Назад до зали
                </button>
            </div>
        );
    }

    return (
        <div className="w-full flex items-center justify-center select-none py-12 px-4">
            <div className="w-full max-w-md bg-[#1a1c26] border border-white/5 p-8 rounded-2xl shadow-2xl animate-fadeIn">
                <div className="text-center mb-8">
                    <h2 className="text-2xl font-black text-[#ffbd14] flex items-center justify-center gap-2 tracking-tight">
                        <Ticket className="w-6 h-6" /> Оплата замовлення
                    </h2>
                    <p className="text-gray-400 text-xs mt-1">Введіть дані картки для підтвердження квитків у StarPlex</p>
                </div>

                {/* Чек транзакції */}
                <div className="bg-[#111219]/50 border border-white/5 p-4 rounded-xl mb-6 flex justify-between items-center text-xs">
                    <div>
                        <p className="text-gray-500 text-[10px] uppercase tracking-wider font-bold">Номер замовлення</p>
                        <p className="font-mono mt-1 text-white font-bold tracking-wide">#{bookingId.substring(0, 8).toUpperCase()}</p>
                    </div>
                    <div className="text-right">
                        <p className="text-gray-500 text-[10px] uppercase tracking-wider font-bold">Сума до сплати</p>
                        <p className="text-xl font-black text-[#ffbd14] mt-0.5">{totalAmount} ₴</p>
                    </div>
                </div>

                <Elements stripe={stripePromise} options={{ clientSecret }}>
                    <CheckoutForm bookingId={bookingId} totalAmount={totalAmount} clientSecret={clientSecret} />
                </Elements>

                <p className="text-[10px] text-center text-gray-500 mt-6 uppercase tracking-wider flex items-center justify-center gap-1 font-bold">
                    <ShieldCheck className="w-3.5 h-3.5 text-emerald-500" /> Усі платежі захищені шифруванням Stripe SSL
                </p>
            </div>
        </div>
    );
};

export default BookingPaymentPage;