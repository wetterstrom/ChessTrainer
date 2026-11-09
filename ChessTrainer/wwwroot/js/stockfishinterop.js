window.stockfishInterop = {
    worker: null,
    currentResolve: null,
    lastScoreCp: 0,
    lastMate: null,

    init: async function() {
        if (this.worker) return;

        // Skapar en WebWorker från CDN utan CORS-problem via Blob
        const workerCode = `importScripts('https://cdnjs.cloudflare.com/ajax/libs/stockfish.js/10.0.2/stockfish.js');`;
        const blob = new Blob([workerCode], { type: 'application/javascript' });
        this.worker = new Worker(URL.createObjectURL(blob));

        this.worker.onmessage = (e) => {
            const line = e.data;
            if (typeof line !== 'string') return;

            // Parsa score (cp eller mate) från UCI info-rader
            if (line.startsWith('info ') && line.includes(' score ')) {
                const cpMatch = line.match(/score cp (-?\d+)/);
                const mateMatch = line.match(/score mate (-?\d+)/);

                if (cpMatch) {
                    this.lastScoreCp = parseInt(cpMatch[1], 10);
                    this.lastMate = null;
                } else if (mateMatch) {
                    this.lastMate = parseInt(mateMatch[1], 10);
                    this.lastScoreCp = null;
                }
            }

            // När motorn är klar kommer "bestmove e2e4"
            if (line.startsWith('bestmove ')) {
                const parts = line.split(' ');
                const bestMove = parts[1] || '';

                if (this.currentResolve) {
                    this.currentResolve({
                        centipawns: this.lastScoreCp,
                        mateInMoves: this.lastMate,
                        bestMoveUci: bestMove
                    });
                    this.currentResolve = null;
                }
            }
        };

        this.worker.postMessage('uci');
        this.worker.postMessage('isready');
    },

    analyze: async function(fen, depth, skillLevel) {
        await this.init();

        return new Promise((resolve) => {
            this.currentResolve = resolve;
            this.lastScoreCp = 0;
            this.lastMate = null;

            this.worker.postMessage('stop');
            this.worker.postMessage(`setoption name Skill Level value ${skillLevel}`);
            this.worker.postMessage(`position fen ${fen}`);
            this.worker.postMessage(`go depth ${depth}`);
        });
    }
};