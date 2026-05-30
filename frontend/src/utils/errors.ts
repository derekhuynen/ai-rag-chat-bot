// Error handling utilities

/**
 * Extract error message from various error types
 */
export function getErrorMessage(error: unknown): string {
  if (!error) return 'An unknown error occurred';

  // Axios error with response data
  if (typeof error === 'object' && error !== null) {
    const err = error as {
      response?: { data?: { error?: string; message?: string } };
      message?: string;
    };

    // Check for response.data.error (our API format)
    if (err.response?.data?.error) {
      return err.response.data.error;
    }

    // Check for response.data.message
    if (err.response?.data?.message) {
      return err.response.data.message;
    }

    // Check for message property
    if (err.message) {
      return err.message;
    }
  }

  // String error
  if (typeof error === 'string') {
    return error;
  }

  return 'An unexpected error occurred';
}

/**
 * Handle async errors with consistent error message extraction
 */
export async function handleAsyncError<T>(
  promise: Promise<T>,
  fallbackMessage: string = 'Operation failed'
): Promise<[Error | null, T | null]> {
  try {
    const data = await promise;
    return [null, data];
  } catch (error) {
    const message = getErrorMessage(error) || fallbackMessage;
    return [new Error(message), null];
  }
}
