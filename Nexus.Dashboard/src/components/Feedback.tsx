type FeedbackProps = {
  message: string;
  isError?: boolean;
};

export function Feedback({ message, isError = false }: FeedbackProps) {
  if (!message.trim()) return null;
  return <p className={`feedback ${isError ? 'error' : 'success'}`}>{message}</p>;
}
