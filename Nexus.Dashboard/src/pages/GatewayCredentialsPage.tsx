import { useEffect, useMemo, useState } from 'react';
import { Eye, Pencil, Plus, Trash2 } from 'lucide-react';
import { zodResolver } from '@hookform/resolvers/zod';
import { useForm } from 'react-hook-form';
import { z } from 'zod';
import {
  addKeyPairCredential,
  addTokenCredential,
  deleteCredential,
  searchGatewayCredentials,
  setCredentialEnabled,
  updateKeyPairCredential,
  updateTokenCredential,
} from '../api/gateways';
import { searchAdministratorAccountsPicker } from '../api/accountPickerSources';
import type { GatewayPrefix, KeyPairCredential, TokenCredential } from '../api/types';
import { AccountPickerDialog } from '@/components/data/entity-picker-dialog';
import { EntityCombobox } from '@/components/data/entity-combobox';
import { PageHeader } from '@/components/layout/page-header';
import { Alert, AlertDescription, AlertTitle } from '@/components/ui/alert';
import { useNotifications } from '../notifications/NotificationContext';
import { maskKey, maskToken, shortId } from '../utils/format';
import {
  AlertDialog,
  AlertDialogAction,
  AlertDialogCancel,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogFooter,
  AlertDialogHeader,
  AlertDialogTitle,
} from '@/components/ui/alert-dialog';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card';
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle,
} from '@/components/ui/dialog';
import {
  Form,
  FormControl,
  FormField,
  FormItem,
  FormLabel,
  FormMessage,
} from '@/components/ui/form';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Switch } from '@/components/ui/switch';
import {
  Table,
  TableBody,
  TableCell,
  TableHead,
  TableHeader,
  TableRow,
} from '@/components/ui/table';
import { Textarea } from '@/components/ui/textarea';

type Variant = GatewayPrefix;

type GatewayConfig = {
  title: string;
  lead: string;
  mode: 'token' | 'keypair';
  addPlaceholder: string;
};

const CONFIG: Record<Variant, GatewayConfig> = {
  frendz: {
    title: 'Frendz',
    lead: 'Credenciais de API com token mascarado; opcionalmente vincule um laranja (conta) à credencial.',
    mode: 'token',
    addPlaceholder: 'Ex.: token dev',
  },
  sigilopay: {
    title: 'SigiloPay',
    lead: 'Credenciais SigiloPay (chave pública e secreta); opcionalmente vincule um laranja (conta).',
    mode: 'keypair',
    addPlaceholder: 'Ex.: produção loja A',
  },
  wintech: {
    title: 'Wintech',
    lead: 'API Wintech Pagamentos — chaves públicas/secretas; opcionalmente vincule um laranja (conta).',
    mode: 'keypair',
    addPlaceholder: 'Ex.: produção loja A',
  },
};

type CredentialRow = TokenCredential & KeyPairCredential;

const credentialBaseSchema = z.object({
  name: z.string(),
  enabled: z.boolean(),
  strawManId: z.string().nullable(),
  strawLabel: z.string().nullable(),
});

const tokenCredentialSchema = credentialBaseSchema.extend({
  token: z.string().trim().min(1, 'O token é obrigatório.'),
});

const keyPairCredentialSchema = credentialBaseSchema.extend({
  publicKey: z.string().trim().min(1, 'Chave pública e secreta são obrigatórias.'),
  secretKey: z.string().trim().min(1, 'Chave pública e secreta são obrigatórias.'),
});

type TokenCredentialValues = z.infer<typeof tokenCredentialSchema>;
type KeyPairCredentialValues = z.infer<typeof keyPairCredentialSchema>;

type GatewayCredentialsPageProps = {
  variant: Variant;
};

export function GatewayCredentialsPage({ variant }: GatewayCredentialsPageProps) {
  const config = CONFIG[variant];
  const { notifyError, notifySuccess } = useNotifications();
  const [credentials, setCredentials] = useState<CredentialRow[]>([]);
  const [search, setSearch] = useState('');
  const [enableToggleBusyId, setEnableToggleBusyId] = useState<string | null>(null);
  const [accountLabels, setAccountLabels] = useState<Record<string, string>>({});

  const [addStrawPickerOpen, setAddStrawPickerOpen] = useState(false);
  const [editStrawPickerOpen, setEditStrawPickerOpen] = useState(false);
  const [addStrawOptions, setAddStrawOptions] = useState<{ id: string; label: string; description?: string }[]>([]);
  const [editStrawOptions, setEditStrawOptions] = useState<{ id: string; label: string; description?: string }[]>([]);

  const [editingId, setEditingId] = useState<string | null>(null);
  const [viewing, setViewing] = useState<CredentialRow | null>(null);
  const [deleteDialogOpen, setDeleteDialogOpen] = useState(false);
  const [deleteId, setDeleteId] = useState('');

  const addTokenForm = useForm<TokenCredentialValues>({
    resolver: zodResolver(tokenCredentialSchema),
    defaultValues: {
      name: '',
      token: '',
      enabled: true,
      strawManId: null,
      strawLabel: null,
    },
  });

  const addKeyPairForm = useForm<KeyPairCredentialValues>({
    resolver: zodResolver(keyPairCredentialSchema),
    defaultValues: {
      name: '',
      publicKey: '',
      secretKey: '',
      enabled: true,
      strawManId: null,
      strawLabel: null,
    },
  });

  const editTokenForm = useForm<TokenCredentialValues>({
    resolver: zodResolver(tokenCredentialSchema),
    defaultValues: {
      name: '',
      token: '',
      enabled: true,
      strawManId: null,
      strawLabel: null,
    },
  });

  const editKeyPairForm = useForm<KeyPairCredentialValues>({
    resolver: zodResolver(keyPairCredentialSchema),
    defaultValues: {
      name: '',
      publicKey: '',
      secretKey: '',
      enabled: true,
      strawManId: null,
      strawLabel: null,
    },
  });

  const filteredCredentials = useMemo(() => {
    if (!search.trim()) return credentials;
    const term = search.trim().toLowerCase();
    return credentials.filter((c) =>
      c.name.toLowerCase().includes(term)
      || c.id.toLowerCase().includes(term)
      || (c.token?.toLowerCase().includes(term) ?? false)
      || (c.publicKey?.toLowerCase().includes(term) ?? false)
      || (c.secretKey?.toLowerCase().includes(term) ?? false)
      || (c.strawManId?.toLowerCase().includes(term) ?? false),
    );
  }, [credentials, search]);

  async function refresh() {
    const result = await searchGatewayCredentials(variant, { limit: 999, offset: 0, keyword: null });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    const items = (result.data?.items ?? []).slice().sort((a, b) => a.name.localeCompare(b.name));
    setCredentials(items);
    setAccountLabels((prev) => {
      const next = { ...prev };
      for (const c of items) {
        if (c.strawManId) next[c.strawManId] = next[c.strawManId] ?? shortId(c.strawManId, 16);
      }
      return next;
    });
  }

  useEffect(() => {
    void refresh();
  }, [variant]);

  function formatStraw(strawManId?: string | null) {
    if (!strawManId) return '— genérico';
    return accountLabels[strawManId] ?? strawManId;
  }

  async function handleAddToken(values: TokenCredentialValues) {
    const result = await addTokenCredential(variant, {
      name: values.name,
      token: values.token,
      strawManId: values.strawManId,
      enabled: values.enabled,
    });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    notifySuccess('Credencial adicionada com sucesso.');
    addTokenForm.reset({
      name: '',
      token: '',
      enabled: true,
      strawManId: null,
      strawLabel: null,
    });
    setAddStrawOptions([]);
    await refresh();
  }

  async function handleAddKeyPair(values: KeyPairCredentialValues) {
    const result = await addKeyPairCredential(variant, {
      name: values.name,
      publicKey: values.publicKey,
      secretKey: values.secretKey,
      strawManId: values.strawManId,
      enabled: values.enabled,
    });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    notifySuccess('Credencial adicionada com sucesso.');
    addKeyPairForm.reset({
      name: '',
      publicKey: '',
      secretKey: '',
      enabled: true,
      strawManId: null,
      strawLabel: null,
    });
    setAddStrawOptions([]);
    await refresh();
  }

  async function handleEnabledToggle(cred: CredentialRow, enabled: boolean) {
    setEnableToggleBusyId(cred.id);
    try {
      const result = await setCredentialEnabled(variant, cred.id, enabled);
      if (!result.ok) {
        notifyError(result.error);
        await refresh();
        return;
      }
      setCredentials((prev) => prev.map((c) => (c.id === cred.id ? { ...c, enabled } : c)));
      notifySuccess(enabled ? 'Credencial habilitada para cobrança.' : 'Credencial desabilitada (não entra no orquestrador).');
    } finally {
      setEnableToggleBusyId(null);
    }
  }

  function beginEdit(cred: CredentialRow) {
    const strawLabel = cred.strawManId
      ? (accountLabels[cred.strawManId] ? `${accountLabels[cred.strawManId]} (${cred.strawManId})` : cred.strawManId)
      : null;
    const common = {
      name: cred.name,
      enabled: cred.enabled,
      strawManId: cred.strawManId ?? null,
      strawLabel,
    };
    if (config.mode === 'token') {
      editTokenForm.reset({
        ...common,
        token: cred.token ?? '',
      });
    } else {
      editKeyPairForm.reset({
        ...common,
        publicKey: cred.publicKey ?? '',
        secretKey: cred.secretKey ?? '',
      });
    }
    if (cred.strawManId && strawLabel) {
      setEditStrawOptions([{ id: cred.strawManId, label: strawLabel, description: cred.strawManId }]);
    } else {
      setEditStrawOptions([]);
    }
    setEditingId(cred.id);
  }

  async function handleUpdateToken(values: TokenCredentialValues) {
    if (!editingId) return;
    const result = await updateTokenCredential(variant, {
      id: editingId,
      name: values.name,
      token: values.token,
      strawManId: values.strawManId,
      enabled: values.enabled,
    });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    notifySuccess('Credencial atualizada com sucesso.');
    setEditingId(null);
    await refresh();
  }

  async function handleUpdateKeyPair(values: KeyPairCredentialValues) {
    if (!editingId) return;
    const result = await updateKeyPairCredential(variant, {
      id: editingId,
      name: values.name,
      publicKey: values.publicKey,
      secretKey: values.secretKey,
      strawManId: values.strawManId,
      enabled: values.enabled,
    });
    if (!result.ok) {
      notifyError(result.error);
      return;
    }
    notifySuccess('Credencial atualizada com sucesso.');
    setEditingId(null);
    await refresh();
  }

  async function confirmDelete() {
    setDeleteDialogOpen(false);
    if (!deleteId) return;
    const result = await deleteCredential(variant, deleteId);
    if (result.ok) notifySuccess('Credencial excluída com sucesso.');
    else notifyError(result.error);
    setDeleteId('');
    setEditingId(null);
    await refresh();
  }

  function renderStrawField(
    form: typeof addTokenForm,
    setPickerOpen: (open: boolean) => void,
    options: { id: string; label: string; description?: string }[],
    setOptions: (options: { id: string; label: string; description?: string }[]) => void,
  ) {
    return (
      <FormField
        control={form.control}
        name="strawManId"
        render={({ field }) => (
          <FormItem className="sm:col-span-2">
            <FormLabel>
              Laranja (conta) <span className="font-normal text-muted-foreground">opcional</span>
            </FormLabel>
            <FormControl>
              <EntityCombobox
                value={field.value}
                onChange={(value) => {
                  field.onChange(value);
                  if (!value) form.setValue('strawLabel', null);
                }}
                options={options}
                placeholder="Genérico (sem laranja)"
                searchPlaceholder="Buscar laranja…"
              />
            </FormControl>
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="outline" size="sm" onClick={() => setPickerOpen(true)}>
                Buscar laranja…
              </Button>
              {field.value ? (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    field.onChange(null);
                    form.setValue('strawLabel', null);
                    setOptions([]);
                  }}
                >
                  Limpar
                </Button>
              ) : null}
            </div>
            <FormMessage />
          </FormItem>
        )}
      />
    );
  }

  function renderKeyPairStrawField(
    form: typeof addKeyPairForm,
    setPickerOpen: (open: boolean) => void,
    options: { id: string; label: string; description?: string }[],
    setOptions: (options: { id: string; label: string; description?: string }[]) => void,
  ) {
    return (
      <FormField
        control={form.control}
        name="strawManId"
        render={({ field }) => (
          <FormItem className="sm:col-span-2">
            <FormLabel>
              Laranja (conta) <span className="font-normal text-muted-foreground">opcional</span>
            </FormLabel>
            <FormControl>
              <EntityCombobox
                value={field.value}
                onChange={(value) => {
                  field.onChange(value);
                  if (!value) form.setValue('strawLabel', null);
                }}
                options={options}
                placeholder="Genérico (sem laranja)"
                searchPlaceholder="Buscar laranja…"
              />
            </FormControl>
            <div className="flex flex-wrap gap-2">
              <Button type="button" variant="outline" size="sm" onClick={() => setPickerOpen(true)}>
                Buscar laranja…
              </Button>
              {field.value ? (
                <Button
                  type="button"
                  variant="ghost"
                  size="sm"
                  onClick={() => {
                    field.onChange(null);
                    form.setValue('strawLabel', null);
                    setOptions([]);
                  }}
                >
                  Limpar
                </Button>
              ) : null}
            </div>
            <FormMessage />
          </FormItem>
        )}
      />
    );
  }

  function renderEnabledField(form: typeof addTokenForm) {
    return (
      <FormField
        control={form.control}
        name="enabled"
        render={({ field }) => (
          <FormItem className="sm:col-span-2">
            <div className="flex items-center gap-2">
              <FormControl>
                <Switch checked={field.value} onCheckedChange={field.onChange} />
              </FormControl>
              <FormLabel className="font-normal">Habilitada para cobrança</FormLabel>
            </div>
            <p className="text-sm text-muted-foreground">
              Desmarque para manter a credencial cadastrada sem usar no orquestrador.
            </p>
          </FormItem>
        )}
      />
    );
  }

  function renderKeyPairEnabledField(form: typeof addKeyPairForm) {
    return (
      <FormField
        control={form.control}
        name="enabled"
        render={({ field }) => (
          <FormItem className="sm:col-span-2">
            <div className="flex items-center gap-2">
              <FormControl>
                <Switch checked={field.value} onCheckedChange={field.onChange} />
              </FormControl>
              <FormLabel className="font-normal">Habilitada para cobrança</FormLabel>
            </div>
            <p className="text-sm text-muted-foreground">
              Desmarque para manter a credencial cadastrada sem usar no orquestrador.
            </p>
          </FormItem>
        )}
      />
    );
  }

  return (
    <div className="space-y-6">
      <PageHeader
        kicker="Integração"
        title={config.title}
        description={config.lead}
        breadcrumbs={[
          { label: 'Dashboard', href: '/dashboard' },
          { label: 'Gateways', href: '/dashboard/gateways' },
          { label: config.title },
        ]}
      />

      <Card className="border-border/60 bg-card/80">
        <CardHeader className="flex-row items-center justify-between gap-3 space-y-0">
          <CardTitle>Adicionar credencial</CardTitle>
          <Badge variant="outline">POST /api/{variant}/credentials</Badge>
        </CardHeader>
        <CardContent className="space-y-4">
          {config.mode === 'token' ? (
            <Form {...addTokenForm}>
              <form className="space-y-4" onSubmit={addTokenForm.handleSubmit(handleAddToken)}>
                <div className="grid gap-4 sm:grid-cols-2">
                  <FormField
                    control={addTokenForm.control}
                    name="name"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel htmlFor="credName">Nome</FormLabel>
                        <FormControl>
                          <Input id="credName" placeholder={config.addPlaceholder} {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={addTokenForm.control}
                    name="token"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel htmlFor="credToken">Token</FormLabel>
                        <FormControl>
                          <Input id="credToken" type="password" placeholder="Cole o token" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  {renderEnabledField(addTokenForm)}
                  {renderStrawField(addTokenForm, setAddStrawPickerOpen, addStrawOptions, setAddStrawOptions)}
                </div>
                <Button type="submit" disabled={addTokenForm.formState.isSubmitting}>
                  <Plus className="size-4" />
                  Adicionar
                </Button>
              </form>
            </Form>
          ) : (
            <Form {...addKeyPairForm}>
              <form className="space-y-4" onSubmit={addKeyPairForm.handleSubmit(handleAddKeyPair)}>
                <div className="grid gap-4 sm:grid-cols-2">
                  <FormField
                    control={addKeyPairForm.control}
                    name="name"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel htmlFor="credName">Nome</FormLabel>
                        <FormControl>
                          <Input id="credName" placeholder={config.addPlaceholder} {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={addKeyPairForm.control}
                    name="publicKey"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel htmlFor="credPublicKey">Chave pública</FormLabel>
                        <FormControl>
                          <Input id="credPublicKey" type="password" placeholder="x-public-key" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  <FormField
                    control={addKeyPairForm.control}
                    name="secretKey"
                    render={({ field }) => (
                      <FormItem>
                        <FormLabel htmlFor="credSecretKey">Chave secreta</FormLabel>
                        <FormControl>
                          <Input id="credSecretKey" type="password" placeholder="x-secret-key" {...field} />
                        </FormControl>
                        <FormMessage />
                      </FormItem>
                    )}
                  />
                  {renderKeyPairEnabledField(addKeyPairForm)}
                  {renderKeyPairStrawField(addKeyPairForm, setAddStrawPickerOpen, addStrawOptions, setAddStrawOptions)}
                </div>
                <Button type="submit" disabled={addKeyPairForm.formState.isSubmitting}>
                  <Plus className="size-4" />
                  Adicionar
                </Button>
              </form>
            </Form>
          )}
        </CardContent>
      </Card>

      <Card className="border-border/60 bg-card/80">
        <CardHeader className="gap-4">
          <div className="flex flex-col gap-2 sm:flex-row sm:items-end">
            <div className="min-w-0 flex-1 space-y-2">
              <Label htmlFor="credSearch">Buscar credenciais</Label>
              <Input
                id="credSearch"
                value={search}
                onChange={(e) => setSearch(e.target.value)}
                placeholder={config.mode === 'token' ? 'Nome, ID ou token…' : 'Nome, ID ou chave…'}
              />
            </div>
            <div className="flex gap-2">
              <Button type="button" variant="outline" onClick={() => setSearch(search)}>Buscar</Button>
              <Button type="button" variant="outline" onClick={() => void refresh()}>Atualizar</Button>
            </div>
          </div>
          <CardTitle>Credenciais cadastradas</CardTitle>
        </CardHeader>
        <CardContent>
          {filteredCredentials.length === 0 ? (
            <Alert>
              <AlertTitle>Nenhuma credencial encontrada</AlertTitle>
              <AlertDescription>Cadastre uma credencial acima ou ajuste a busca.</AlertDescription>
            </Alert>
          ) : (
            <Table>
              <TableHeader>
                <TableRow>
                  <TableHead>Nome</TableHead>
                  {config.mode === 'token' ? (
                    <TableHead>Token</TableHead>
                  ) : (
                    <>
                      <TableHead>Chave pública</TableHead>
                      <TableHead>Chave secreta</TableHead>
                    </>
                  )}
                  <TableHead>Laranja</TableHead>
                  <TableHead>Ativa</TableHead>
                  <TableHead className="text-right">Ações</TableHead>
                </TableRow>
              </TableHeader>
              <TableBody>
                {filteredCredentials.map((cred) => (
                  <TableRow key={cred.id}>
                    <TableCell>{cred.name}</TableCell>
                    {config.mode === 'token' ? (
                      <TableCell>
                        <span className="font-mono text-xs" title="Mascarado">{maskToken(cred.token ?? '')}</span>
                      </TableCell>
                    ) : (
                      <>
                        <TableCell>
                          <span className="font-mono text-xs" title="Mascarado">{maskKey(cred.publicKey ?? '')}</span>
                        </TableCell>
                        <TableCell>
                          <span className="font-mono text-xs" title="Mascarado">{maskKey(cred.secretKey ?? '')}</span>
                        </TableCell>
                      </>
                    )}
                    <TableCell className="text-muted-foreground">{formatStraw(cred.strawManId)}</TableCell>
                    <TableCell>
                      <label className="flex items-center gap-2">
                        <Switch
                          checked={cred.enabled}
                          disabled={enableToggleBusyId === cred.id}
                          onCheckedChange={(checked) => void handleEnabledToggle(cred, checked)}
                        />
                        <span className="text-sm text-muted-foreground">{cred.enabled ? 'Sim' : 'Não'}</span>
                      </label>
                    </TableCell>
                    <TableCell className="text-right">
                      <div className="inline-flex items-center gap-1">
                        <Button type="button" variant="ghost" size="icon-sm" title="Ver credencial" aria-label="Ver credencial" onClick={() => setViewing(cred)}>
                          <Eye className="size-4" />
                        </Button>
                        <Button type="button" variant="ghost" size="icon-sm" title="Editar" aria-label="Editar credencial" onClick={() => beginEdit(cred)}>
                          <Pencil className="size-4" />
                        </Button>
                        <Button
                          type="button"
                          variant="ghost"
                          size="icon-sm"
                          className="text-destructive hover:text-destructive"
                          title="Excluir"
                          aria-label="Excluir credencial"
                          onClick={() => { setDeleteId(cred.id); setDeleteDialogOpen(true); }}
                        >
                          <Trash2 className="size-4" />
                        </Button>
                      </div>
                    </TableCell>
                  </TableRow>
                ))}
              </TableBody>
            </Table>
          )}
        </CardContent>
      </Card>

      <Dialog open={editingId !== null} onOpenChange={(isOpen) => { if (!isOpen) setEditingId(null); }}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Editar credencial</DialogTitle>
            <DialogDescription>PUT /api/{variant}/credentials</DialogDescription>
          </DialogHeader>
          {config.mode === 'token' ? (
            <Form {...editTokenForm}>
              <form className="grid gap-4 sm:grid-cols-2" onSubmit={editTokenForm.handleSubmit(handleUpdateToken)}>
                <FormField
                  control={editTokenForm.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem className="sm:col-span-2">
                      <FormLabel htmlFor="editCredName">Nome</FormLabel>
                      <FormControl>
                        <Input id="editCredName" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={editTokenForm.control}
                  name="token"
                  render={({ field }) => (
                    <FormItem className="sm:col-span-2">
                      <FormLabel htmlFor="editCredToken">Token</FormLabel>
                      <FormControl>
                        <Input id="editCredToken" type="password" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                {renderStrawField(editTokenForm, setEditStrawPickerOpen, editStrawOptions, setEditStrawOptions)}
                {renderEnabledField(editTokenForm)}
                <DialogFooter className="sm:col-span-2">
                  <Button type="button" variant="outline" onClick={() => setEditingId(null)}>Cancelar</Button>
                  <Button type="submit" disabled={editTokenForm.formState.isSubmitting}>Salvar</Button>
                </DialogFooter>
              </form>
            </Form>
          ) : (
            <Form {...editKeyPairForm}>
              <form className="grid gap-4 sm:grid-cols-2" onSubmit={editKeyPairForm.handleSubmit(handleUpdateKeyPair)}>
                <FormField
                  control={editKeyPairForm.control}
                  name="name"
                  render={({ field }) => (
                    <FormItem className="sm:col-span-2">
                      <FormLabel htmlFor="editCredName">Nome</FormLabel>
                      <FormControl>
                        <Input id="editCredName" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={editKeyPairForm.control}
                  name="publicKey"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel htmlFor="editPublicKey">Chave pública</FormLabel>
                      <FormControl>
                        <Input id="editPublicKey" type="password" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                <FormField
                  control={editKeyPairForm.control}
                  name="secretKey"
                  render={({ field }) => (
                    <FormItem>
                      <FormLabel htmlFor="editSecretKey">Chave secreta</FormLabel>
                      <FormControl>
                        <Input id="editSecretKey" type="password" {...field} />
                      </FormControl>
                      <FormMessage />
                    </FormItem>
                  )}
                />
                {renderKeyPairStrawField(editKeyPairForm, setEditStrawPickerOpen, editStrawOptions, setEditStrawOptions)}
                {renderKeyPairEnabledField(editKeyPairForm)}
                <DialogFooter className="sm:col-span-2">
                  <Button type="button" variant="outline" onClick={() => setEditingId(null)}>Cancelar</Button>
                  <Button type="submit" disabled={editKeyPairForm.formState.isSubmitting}>Salvar</Button>
                </DialogFooter>
              </form>
            </Form>
          )}
        </DialogContent>
      </Dialog>

      <Dialog open={viewing !== null} onOpenChange={(isOpen) => { if (!isOpen) setViewing(null); }}>
        <DialogContent className="sm:max-w-lg">
          <DialogHeader>
            <DialogTitle>Credencial</DialogTitle>
          </DialogHeader>
          {viewing ? (
            <div className="space-y-3">
              <p className="text-sm text-muted-foreground">
                <strong className="text-foreground">ID:</strong>{' '}
                <span className="font-mono">{viewing.id}</span>
              </p>
              <p className="text-sm text-muted-foreground">
                <strong className="text-foreground">Nome:</strong> {viewing.name}
              </p>
              <p className="text-sm text-muted-foreground">
                <strong className="text-foreground">Cobrança:</strong>{' '}
                {viewing.enabled ? 'Habilitada' : 'Desabilitada'}
              </p>
              {config.mode === 'token' ? (
                <div className="space-y-2">
                  <Label>Token</Label>
                  <Textarea readOnly rows={5} value={viewing.token ?? ''} className="font-mono text-xs" />
                </div>
              ) : (
                <>
                  <div className="space-y-2">
                    <Label>Chave pública</Label>
                    <Textarea readOnly rows={3} value={viewing.publicKey ?? ''} className="font-mono text-xs" />
                  </div>
                  <div className="space-y-2">
                    <Label>Chave secreta</Label>
                    <Textarea readOnly rows={3} value={viewing.secretKey ?? ''} className="font-mono text-xs" />
                  </div>
                </>
              )}
            </div>
          ) : null}
        </DialogContent>
      </Dialog>

      <AccountPickerDialog
        open={addStrawPickerOpen}
        onClose={() => setAddStrawPickerOpen(false)}
        searchAccounts={searchAdministratorAccountsPicker}
        title="Laranja para credencial"
        subtitle="Opcional. Credenciais sem laranja participam como genéricas no filtro de cobrança."
        onSelected={(row) => {
          const label = `${row.username} (${row.id})`;
          if (config.mode === 'token') {
            addTokenForm.setValue('strawManId', row.id);
            addTokenForm.setValue('strawLabel', label);
          } else {
            addKeyPairForm.setValue('strawManId', row.id);
            addKeyPairForm.setValue('strawLabel', label);
          }
          setAddStrawOptions([{ id: row.id, label: row.username, description: row.id }]);
          setAccountLabels((prev) => ({ ...prev, [row.id]: row.username }));
        }}
      />

      <AccountPickerDialog
        open={editStrawPickerOpen}
        onClose={() => setEditStrawPickerOpen(false)}
        searchAccounts={searchAdministratorAccountsPicker}
        title="Laranja para credencial"
        subtitle="Vincule uma conta ou deixe genérico."
        onSelected={(row) => {
          if (config.mode === 'token') {
            editTokenForm.setValue('strawManId', row.id);
            editTokenForm.setValue('strawLabel', `${row.username} (${row.id})`);
          } else {
            editKeyPairForm.setValue('strawManId', row.id);
            editKeyPairForm.setValue('strawLabel', `${row.username} (${row.id})`);
          }
          setEditStrawOptions([{ id: row.id, label: row.username, description: row.id }]);
          setAccountLabels((prev) => ({ ...prev, [row.id]: row.username }));
        }}
      />

      <AlertDialog open={deleteDialogOpen} onOpenChange={(open) => { if (!open) { setDeleteDialogOpen(false); setDeleteId(''); } }}>
        <AlertDialogContent>
          <AlertDialogHeader>
            <AlertDialogTitle>Confirmar exclusão</AlertDialogTitle>
            <AlertDialogDescription>
              Tem certeza que deseja excluir esta credencial?
            </AlertDialogDescription>
          </AlertDialogHeader>
          <AlertDialogFooter>
            <AlertDialogCancel>Cancelar</AlertDialogCancel>
            <AlertDialogAction onClick={() => void confirmDelete()}>Confirmar</AlertDialogAction>
          </AlertDialogFooter>
        </AlertDialogContent>
      </AlertDialog>
    </div>
  );
}
