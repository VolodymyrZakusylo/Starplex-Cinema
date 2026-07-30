import React from 'react';

interface MovieTrailerProps {
    videoId: string | null;
    movieTitle: string;
}

export const MovieTrailer: React.FC<MovieTrailerProps> = ({ videoId, movieTitle }) => {
    if (!videoId) return null;

    return (
        <div className="lg:col-span-2 text-xs">
            <h2 className="text-xl font-black mb-1 tracking-tight">Трейлер фільму</h2>
            <p className="text-gray-400 text-xs mb-6">Офіційний промо-відеоролик кінокартини з YouTube.</p>
            <div className="w-full aspect-video rounded-2xl overflow-hidden border border-white/5 bg-black shadow-xl">
                <iframe
                    className="w-full h-full border-none"
                    src={`https://www.youtube.com/embed/${videoId}?controls=1&modestbranding=1&rel=0`}
                    title={`${movieTitle} - Трейлер`}
                    allow="accelerometer; autoplay; clipboard-write; encrypted-media; gyroscope; picture-in-picture; web-share"
                    allowFullScreen
                ></iframe>
            </div>
        </div>
    );
};