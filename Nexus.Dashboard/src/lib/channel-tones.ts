import { cva, type VariantProps } from 'class-variance-authority';
import { cn } from '@/lib/utils';

export const channelToneVariants = cva('border font-medium', {
  variants: {
    tone: {
      prod: 'border-success/35 bg-success/10 text-success',
      staging: 'border-warning/35 bg-warning/10 text-warning',
      development: 'border-primary/35 bg-primary/10 text-primary',
      accent: 'border-warning/35 bg-warning/10 text-warning',
      custom: 'border-border bg-muted/30 text-foreground',
    },
    size: {
      sm: 'rounded-full px-2 py-0.5 text-xs',
      md: 'rounded-lg px-2.5 py-1 text-sm',
    },
  },
  defaultVariants: {
    tone: 'custom',
    size: 'sm',
  },
});

export type ChannelTone = NonNullable<VariantProps<typeof channelToneVariants>['tone']>;

export function channelToneFromRoute(routeValue: string): ChannelTone {
  const normalized = routeValue.toLowerCase();
  if (normalized.includes('prod')) return 'prod';
  if (normalized.includes('stag')) return 'staging';
  if (normalized.includes('dev')) return 'development';
  return 'custom';
}

export function channelToneClass(tone: ChannelTone, size: 'sm' | 'md' = 'sm') {
  return cn(channelToneVariants({ tone, size }));
}
