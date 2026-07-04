/**
 * Externally completable promise (like C# TaskCompletionSource).
 */
export class CompletionSource {
    /** @type {Promise<any>} */
    #promise;

    /** @type {(value?: any) => void} */
    #resolve;

    /** @type {(reason?: any) => void} */
    #reject;

    /** @type {boolean} */
    #completed = false;

    constructor() {
        this.#promise = new Promise((resolve, reject) => {
            this.#resolve = resolve;
            this.#reject = reject;
        });
    }

    /** @returns {Promise<any>} */
    get promise() {
        return this.#promise;
    }

    /** @returns {boolean} */
    get isCompleted() {
        return this.#completed;
    }

    /**
     * @param {any} [value]
     * @throws {Error} if already completed
     */
    resolve(value) {
        if (this.#completed) {
            throw new Error("CompletionSource already completed");
        }
        this.#completed = true;
        this.#resolve(value);
    }

    /**
     * @param {any} [reason]
     * @throws {Error} if already completed
     */
    reject(reason) {
        if (this.#completed) {
            throw new Error("CompletionSource already completed");
        }
        this.#completed = true;
        this.#reject(reason);
    }

    /**
     * @param {any} [value]
     * @returns {boolean}
     */
    tryResolve(value) {
        if (this.#completed) {
            return false;
        }
        this.#completed = true;
        this.#resolve(value);
        return true;
    }

    /**
     * @param {any} [reason]
     * @returns {boolean}
     */
    tryReject(reason) {
        if (this.#completed) {
            return false;
        }
        this.#completed = true;
        this.#reject(reason);
        return true;
    }
}
