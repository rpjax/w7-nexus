import { BrandMark } from '@/components/brand/BrandMark';
import { Card, CardContent, CardHeader } from '@/components/ui/card';

export function AuthLoadingCard({ message }: { message: string }) {
  return (
    <div className="flex min-h-dvh items-center justify-center p-4" role="status" aria-live="polite">
      <Card className="w-full max-w-sm border-border/60 bg-card/90 backdrop-blur-md">
        <CardHeader className="items-center text-center">
          <BrandMark size="md" />
        </CardHeader>
        <CardContent>
          <p className="text-center text-sm text-muted-foreground">{message}</p>
        </CardContent>
      </Card>
    </div>
  );
}
