namespace Nexus.AccountNodes.Aggregates;

[AttributeUsage(AttributeTargets.Field)]
public sealed class BrazilianBankMetadataAttribute(string name, string code, string ispb) : Attribute
{
    public string Name { get; } = name;
    public string Code { get; } = code;
    public string Ispb { get; } = ispb;
}

public enum BrazilianBank
{
    [BrazilianBankMetadata("Acesso Solucoes De Pagamento S.A.", "332", "13140088")]
    AcessoSolucoesDePagamentoSA_332 = 1,
    [BrazilianBankMetadata("Advanced Corretora De Cambio Ltda", "117", "92856905")]
    AdvancedCorretoraDeCambioLtda_117 = 2,
    [BrazilianBankMetadata("Avista S.A. Credito", "280", "23862762")]
    AvistaSACredito_280 = 3,
    [BrazilianBankMetadata("Agk Corretora De Cambio S.A.", "272", "250699")]
    AgkCorretoraDeCambioSA_272 = 4,
    [BrazilianBankMetadata("Al5 S.A. Credito", "349", "27214112")]
    Al5SACredito_349 = 5,
    [BrazilianBankMetadata("Amazonia Corretora De Cambio Ltda.", "313", "16927221")]
    AmazoniaCorretoraDeCambioLtda_313 = 6,
    [BrazilianBankMetadata("Ativa Investimentos S.A. Corretora De Titulos, Cambio E Valores", "188", "33775974")]
    AtivaInvestimentosSACorretoraDeTitulosCambioEValores_188 = 7,
    [BrazilianBankMetadata("B&T Corretora De Cambio Ltda.", "080", "73622748")]
    BTCorretoraDeCambioLtda_080 = 8,
    [BrazilianBankMetadata("Banco Abc Brasil S.A.", "246", "28195667")]
    BancoAbcBrasilSA_246 = 9,
    [BrazilianBankMetadata("Banco Abn Amro S.A.", "075", "3532415")]
    BancoAbnAmroSA_075 = 10,
    [BrazilianBankMetadata("Banco Agibank S.A.", "121", "10664513")]
    BancoAgibankSA_121 = 11,
    [BrazilianBankMetadata("Banco Alfa S.A.", "025", "3323840")]
    BancoAlfaSA_025 = 12,
    [BrazilianBankMetadata("Banco Andbank (Brasil) S.A.", "065", "48795256")]
    BancoAndbankBrasilSA_065 = 13,
    [BrazilianBankMetadata("Banco Arbi S.A.", "213", "54403563")]
    BancoArbiSA_213 = 14,
    [BrazilianBankMetadata("Banco B3 S.A.", "096", "997185")]
    BancoB3SA_096 = 15,
    [BrazilianBankMetadata("Banco Bandepe S.A.", "024", "10866788")]
    BancoBandepeSA_024 = 16,
    [BrazilianBankMetadata("Banco Bari De Investimentos E Financiamentos S.A.", "330", "556603")]
    BancoBariDeInvestimentosEFinanciamentosSA_330 = 17,
    [BrazilianBankMetadata("Banco BMG S.A.", "318", "61186680")]
    BancoBMGSA_318 = 18,
    [BrazilianBankMetadata("Banco Bnp Paribas Brasil S.A.", "752", "1522368")]
    BancoBnpParibasBrasilSA_752 = 19,
    [BrazilianBankMetadata("Banco Bocom Bbm S.A.", "107", "15114366")]
    BancoBocomBbmSA_107 = 20,
    [BrazilianBankMetadata("Banco Bradescard S.A.", "063", "4184779")]
    BancoBradescardSA_063 = 21,
    [BrazilianBankMetadata("Banco Bradesco BBI S.A.", "036", "6271464")]
    BancoBradescoBBISA_036 = 22,
    [BrazilianBankMetadata("Banco Bradesco Berj S.A.", "122", "33147315")]
    BancoBradescoBerjSA_122 = 23,
    [BrazilianBankMetadata("Banco Bradesco Financiamentos S.A.", "394", "7207996")]
    BancoBradescoFinanciamentosSA_394 = 24,
    [BrazilianBankMetadata("Banco Bradesco S.A.", "237", "60746948")]
    BancoBradescoSA_237 = 25,
    [BrazilianBankMetadata("Banco Bs2 S.A.", "218", "71027866")]
    BancoBs2SA_218 = 26,
    [BrazilianBankMetadata("Banco BTG Pactual S.A.", "208", "30306294")]
    BancoBTGPactualSA_208 = 27,
    [BrazilianBankMetadata("Banco C6 Consignado S.A.", "626", "61348538")]
    BancoC6ConsignadoSA_626 = 28,
    [BrazilianBankMetadata("Banco C6 S.A.", "336", "31872495")]
    BancoC6SA_336 = 29,
    [BrazilianBankMetadata("Banco Caixa Geral - Brasil S.A.", "473", "33466988")]
    BancoCaixaGeralBrasilSA_473 = 30,
    [BrazilianBankMetadata("Banco Capital S.A.", "412", "15173776")]
    BancoCapitalSA_412 = 31,
    [BrazilianBankMetadata("Banco Cargill S.A.", "040", "3609817")]
    BancoCargillSA_040 = 32,
    [BrazilianBankMetadata("Banco Cedula S.A.", "266", "33132044")]
    BancoCedulaSA_266 = 33,
    [BrazilianBankMetadata("Banco Cetelem S.A. 739", "739", "558456")]
    BancoCetelemSA739_739 = 34,
    [BrazilianBankMetadata("Banco Cetelem S.A. 233", "233", "62421979")]
    BancoCetelemSA233_233 = 35,
    [BrazilianBankMetadata("Banco Citibank S.A.", "745", "33479023")]
    BancoCitibankSA_745 = 36,
    [BrazilianBankMetadata("Banco Classico S.A.", "241", "31597552")]
    BancoClassicoSA_241 = 37,
    [BrazilianBankMetadata("Banco Cooperativo Do Brasil S.A. - Bancoob - Sicoob", "756", "2038232")]
    BancoCooperativoDoBrasilSABancoobSicoob_756 = 38,
    [BrazilianBankMetadata("Banco Cooperativo Sicredi S.A.", "748", "1181521")]
    BancoCooperativoSicrediSA_748 = 39,
    [BrazilianBankMetadata("Banco Credit Agricole Brasil S.A.", "222", "75647891")]
    BancoCreditAgricoleBrasilSA_222 = 40,
    [BrazilianBankMetadata("Banco Credit Suisse (Brasil) S.A.", "505", "32062580")]
    BancoCreditSuisseBrasilSA_505 = 41,
    [BrazilianBankMetadata("Banco Crefisa S.A.", "069", "61033106")]
    BancoCrefisaSA_069 = 42,
    [BrazilianBankMetadata("Banco CSF S.A.", "368", "8357240")]
    BancoCSFSA_368 = 43,
    [BrazilianBankMetadata("Banco da Amazonia S.A.", "003", "4902979")]
    BancodaAmazoniaSA_003 = 44,
    [BrazilianBankMetadata("Banco da China Brasil S.A.", "083", "10690848")]
    BancodaChinaBrasilSA_083 = 45,
    [BrazilianBankMetadata("Banco Daycoval S.A.", "707", "62232889")]
    BancoDaycovalSA_707 = 46,
    [BrazilianBankMetadata("Banco De La Nacion Argentina", "300", "33042151")]
    BancoDeLaNacionArgentina_300 = 47,
    [BrazilianBankMetadata("Banco De La Provincia De Buenos Aires", "495", "44189447")]
    BancoDeLaProvinciaDeBuenosAires_495 = 48,
    [BrazilianBankMetadata("Banco Digimais S.A.", "654", "92874270")]
    BancoDigimaisSA_654 = 49,
    [BrazilianBankMetadata("Banco Digio S.A.", "335", "27098060")]
    BancoDigioSA_335 = 50,
    [BrazilianBankMetadata("Banco do Brasil S.A.", "001", "00000000")]
    BancodoBrasilSA_001 = 51,
    [BrazilianBankMetadata("Banco Do Estado De Sergipe S.A.", "047", "13009717")]
    BancoDoEstadoDeSergipeSA_047 = 52,
    [BrazilianBankMetadata("Banco Do Estado Do Para S.A.", "037", "4913711")]
    BancoDoEstadoDoParaSA_037 = 53,
    [BrazilianBankMetadata("Banco Do Estado Do Rio Grande Do Sul S.A.", "041", "92702067")]
    BancoDoEstadoDoRioGrandeDoSulSA_041 = 54,
    [BrazilianBankMetadata("Banco Do Nordeste Do Brasil S.A.", "004", "7237373")]
    BancoDoNordesteDoBrasilSA_004 = 55,
    [BrazilianBankMetadata("Banco Fator S.A.", "265", "33644196")]
    BancoFatorSA_265 = 56,
    [BrazilianBankMetadata("Banco Fibra S.A.", "224", "58616418")]
    BancoFibraSA_224 = 57,
    [BrazilianBankMetadata("Banco Finaxis S.A.", "094", "11758741")]
    BancoFinaxisSA_094 = 58,
    [BrazilianBankMetadata("Banco Gm S.A.", "390", "59274605")]
    BancoGmSA_390 = 59,
    [BrazilianBankMetadata("Banco Guanabara S.A.", "612", "31880826")]
    BancoGuanabaraSA_612 = 60,
    [BrazilianBankMetadata("Banco HSBC S.A.", "269", "53518684")]
    BancoHSBCSA_269 = 61,
    [BrazilianBankMetadata("Banco Inbursa S.A.", "012", "4866275")]
    BancoInbursaSA_012 = 62,
    [BrazilianBankMetadata("Banco Industrial Do Brasil S.A.", "604", "31895683")]
    BancoIndustrialDoBrasilSA_604 = 63,
    [BrazilianBankMetadata("Banco Indusval S.A.", "653", "61024352")]
    BancoIndusvalSA_653 = 64,
    [BrazilianBankMetadata("Banco Inter S.A.", "077", "416968")]
    BancoInterSA_077 = 65,
    [BrazilianBankMetadata("Banco Investcred Unibanco S.A.", "249", "61182408")]
    BancoInvestcredUnibancoSA_249 = 66,
    [BrazilianBankMetadata("Banco Itau BBA S.A.", "184", "17298092")]
    BancoItauBBASA_184 = 67,
    [BrazilianBankMetadata("Banco Itau Consignado S.A.", "029", "33885724")]
    BancoItauConsignadoSA_029 = 68,
    [BrazilianBankMetadata("Banco Itaubank S.A.", "479", "60394079")]
    BancoItaubankSA_479 = 69,
    [BrazilianBankMetadata("Banco J. Safra S.A.", "074", "3017677")]
    BancoJSafraSA_074 = 70,
    [BrazilianBankMetadata("Banco J.P. Morgan S.A.", "376", "33172537")]
    BancoJPMorganSA_376 = 71,
    [BrazilianBankMetadata("Banco John Deere S.A.", "217", "91884981")]
    BancoJohnDeereSA_217 = 72,
    [BrazilianBankMetadata("Banco Kdb Do Brasil S.A.", "076", "7656500")]
    BancoKdbDoBrasilSA_076 = 73,
    [BrazilianBankMetadata("Banco Keb Hana Do Brasil S.A.", "757", "2318507")]
    BancoKebHanaDoBrasilSA_757 = 74,
    [BrazilianBankMetadata("Banco Luso Brasileiro S.A.", "600", "59118133")]
    BancoLusoBrasileiroSA_600 = 75,
    [BrazilianBankMetadata("Banco Maxima S.A.", "243", "33923798")]
    BancoMaximaSA_243 = 76,
    [BrazilianBankMetadata("Banco Mercantil do Brasil S.A.", "389", "17184037")]
    BancoMercantildoBrasilSA_389 = 77,
    [BrazilianBankMetadata("Banco Mercedes-Benz Do Brasil S.A.", "381", "60814191")]
    BancoMercedesBenzDoBrasilSA_381 = 78,
    [BrazilianBankMetadata("Banco Mizuho Do Brasil S.A.", "370", "61088183")]
    BancoMizuhoDoBrasilSA_370 = 79,
    [BrazilianBankMetadata("Banco Modal S.A.", "746", "30723886")]
    BancoModalSA_746 = 80,
    [BrazilianBankMetadata("Banco Morgan Stanley S.A.", "066", "2801938")]
    BancoMorganStanleySA_066 = 81,
    [BrazilianBankMetadata("Banco Mufg Brasil S.A.", "456", "60498557")]
    BancoMufgBrasilSA_456 = 82,
    [BrazilianBankMetadata("Banco Nacional De Desenvolvimento Economico E Social", "007", "33657248")]
    BancoNacionalDeDesenvolvimentoEconomicoESocial_007 = 83,
    [BrazilianBankMetadata("Banco Ole Consignado S.A.", "169", "71371686")]
    BancoOleConsignadoSA_169 = 84,
    [BrazilianBankMetadata("Banco Original Do Agronegocio S.A.", "079", "9516419")]
    BancoOriginalDoAgronegocioSA_079 = 85,
    [BrazilianBankMetadata("Banco Original S.A.", "212", "92894922")]
    BancoOriginalSA_212 = 86,
    [BrazilianBankMetadata("Banco Ourinvest S.A.", "712", "78632767")]
    BancoOurinvestSA_712 = 87,
    [BrazilianBankMetadata("Banco Pan S.A.", "623", "59285411")]
    BancoPanSA_623 = 88,
    [BrazilianBankMetadata("Banco Paulista S.A.", "611", "61820817")]
    BancoPaulistaSA_611 = 89,
    [BrazilianBankMetadata("Banco Pine S.A.", "643", "62144175")]
    BancoPineSA_643 = 90,
    [BrazilianBankMetadata("Banco Rabobank International Brasil S.A.", "747", "1023570")]
    BancoRabobankInternationalBrasilSA_747 = 91,
    [BrazilianBankMetadata("Banco Randon S.A.", "088", "11476673")]
    BancoRandonSA_088 = 92,
    [BrazilianBankMetadata("Banco Rendimento S.A.", "633", "68900810")]
    BancoRendimentoSA_633 = 93,
    [BrazilianBankMetadata("Banco Ribeirao Preto S.A.", "741", "517645")]
    BancoRibeiraoPretoSA_741 = 94,
    [BrazilianBankMetadata("Banco Rodobens S.A.", "120", "33603457")]
    BancoRodobensSA_120 = 95,
    [BrazilianBankMetadata("Banco Safra S.A.", "422", "58160789")]
    BancoSafraSA_422 = 96,
    [BrazilianBankMetadata("Banco Santander (Brasil) S.A.", "033", "90400888")]
    BancoSantanderBrasilSA_033 = 97,
    [BrazilianBankMetadata("Banco Semear S.A.", "743", "795423")]
    BancoSemearSA_743 = 98,
    [BrazilianBankMetadata("Banco Sistema S.A.", "754", "76543115")]
    BancoSistemaSA_754 = 99,
    [BrazilianBankMetadata("Banco Smartbank S.A.", "630", "58497702")]
    BancoSmartbankSA_630 = 100,
    [BrazilianBankMetadata("Banco Societe Generale Brasil S.A.", "366", "61533584")]
    BancoSocieteGeneraleBrasilSA_366 = 101,
    [BrazilianBankMetadata("Banco Sofisa S.A.", "637", "60889128")]
    BancoSofisaSA_637 = 102,
    [BrazilianBankMetadata("Banco Sumitomo Mitsui Brasileiro S.A.", "464", "60518222")]
    BancoSumitomoMitsuiBrasileiroSA_464 = 103,
    [BrazilianBankMetadata("Banco Topazio S.A.", "082", "7679404")]
    BancoTopazioSA_082 = 104,
    [BrazilianBankMetadata("Banco Toyota Do Brasil S.A.", "387", "3215790")]
    BancoToyotaDoBrasilSA_387 = 105,
    [BrazilianBankMetadata("Banco Triangulo S.A.", "634", "17351180")]
    BancoTrianguloSA_634 = 106,
    [BrazilianBankMetadata("Banco Tricury S.A.", "018", "57839805")]
    BancoTricurySA_018 = 107,
    [BrazilianBankMetadata("Banco Volkswagen S.A.", "393", "59109165")]
    BancoVolkswagenSA_393 = 108,
    [BrazilianBankMetadata("Banco Votorantim S.A.", "655", "59588111")]
    BancoVotorantimSA_655 = 109,
    [BrazilianBankMetadata("Banco Vr S.A.", "610", "78626983")]
    BancoVrSA_610 = 110,
    [BrazilianBankMetadata("Banco Western Union Do Brasil S.A.", "119", "13720915")]
    BancoWesternUnionDoBrasilSA_119 = 111,
    [BrazilianBankMetadata("Banco Woori Bank Do Brasil S.A.", "124", "15357060")]
    BancoWooriBankDoBrasilSA_124 = 112,
    [BrazilianBankMetadata("Banco Xp S.A.", "348", "33264668")]
    BancoXpSA_348 = 113,
    [BrazilianBankMetadata("Bancoseguro S.A.", "081", "10264663")]
    BancoseguroSA_081 = 114,
    [BrazilianBankMetadata("Banestes S.A. Banco Do Estado do Espirito Santo", "021", "28127603")]
    BanestesSABancoDoEstadodoEspiritoSanto_021 = 115,
    [BrazilianBankMetadata("Bank of America Merrill Lynch Banco Multiplo S.A.", "755", "62073200")]
    BankofAmericaMerrillLynchBancoMultiploSA_755 = 116,
    [BrazilianBankMetadata("Bari Companhia Hipotecaria", "268", "14511781")]
    BariCompanhiaHipotecaria_268 = 117,
    [BrazilianBankMetadata("Bbc Leasing S.A. - Arrendamento Mercantil", "378", "1852137")]
    BbcLeasingSAArrendamentoMercantil_378 = 118,
    [BrazilianBankMetadata("Bcv - Banco De Credito E Varejo S.A.", "250", "50585090")]
    BcvBancoDeCreditoEVarejoSA_250 = 119,
    [BrazilianBankMetadata("Bexs Banco De Cambio S/A", "144", "13059145")]
    BexsBancoDeCambioSA_144 = 120,
    [BrazilianBankMetadata("Bexs Corretora De Cambio S/A", "253", "52937216")]
    BexsCorretoraDeCambioSA_253 = 121,
    [BrazilianBankMetadata("Bgc Liquidez Distribuidora De Titulos E Valores Mobiliarios Ltda", "134", "33862244")]
    BgcLiquidezDistribuidoraDeTitulosEValoresMobiliariosLtda_134 = 122,
    [BrazilianBankMetadata("Bny Mellon Banco S.A.", "017", "42272526")]
    BnyMellonBancoSA_017 = 123,
    [BrazilianBankMetadata("Bonuscred Sociedade De Credito Direto S.A.", "408", "36586946")]
    BonuscredSociedadeDeCreditoDiretoSA_408 = 124,
    [BrazilianBankMetadata("Bpp Instituicao De Pagamento S.A.", "301", "13370835")]
    BppInstituicaoDePagamentoSA_301 = 125,
    [BrazilianBankMetadata("Br Partners Banco De Investimento S.A.", "126", "13220493")]
    BrPartnersBancoDeInvestimentoSA_126 = 126,
    [BrazilianBankMetadata("BrB - Banco De Brasilia S.A.", "070", "208")]
    BrBBancoDeBrasiliaSA_070 = 127,
    [BrazilianBankMetadata("Brk S.A. Credito", "092", "12865507")]
    BrkSACredito_092 = 128,
    [BrazilianBankMetadata("Brl Trust Distribuidora De Titulos E Valores Mobiliarios S.A.", "173", "13486793")]
    BrlTrustDistribuidoraDeTitulosEValoresMobiliariosSA_173 = 129,
    [BrazilianBankMetadata("Broker Brasil Corretora De Cambio Ltda.", "142", "16944141")]
    BrokerBrasilCorretoraDeCambioLtda_142 = 130,
    [BrazilianBankMetadata("Bs2 Distribuidora De Titulos E Valores Mobiliarios S.A.", "292", "28650236")]
    Bs2DistribuidoraDeTitulosEValoresMobiliariosSA_292 = 131,
    [BrazilianBankMetadata("Caixa Economica Federal", "104", "360305")]
    CaixaEconomicaFederal_104 = 132,
    [BrazilianBankMetadata("Cambionet Corretora De Cambio Ltda.", "309", "14190547")]
    CambionetCorretoraDeCambioLtda_309 = 133,
    [BrazilianBankMetadata("Carol Distribuidora De Titulos E Valores Mobiliarios Ltda.", "288", "62237649")]
    CarolDistribuidoraDeTitulosEValoresMobiliariosLtda_288 = 134,
    [BrazilianBankMetadata("Cartos Sociedade De Credito Direto S.A.", "324", "21332862")]
    CartosSociedadeDeCreditoDiretoSA_324 = 135,
    [BrazilianBankMetadata("Caruana S.A. - Sociedade De Credito", "130", "9313766")]
    CaruanaSASociedadeDeCredito_130 = 136,
    [BrazilianBankMetadata("Casa Do Credito S.A. Sociedade De Credito Ao Microempreendedor", "159", "5442029")]
    CasaDoCreditoSASociedadeDeCreditoAoMicroempreendedor_159 = 137,
    [BrazilianBankMetadata("Central Cooperativa De Credito No Estado Do Espirito Santo - Cecoop", "114", "5790149")]
    CentralCooperativaDeCreditoNoEstadoDoEspiritoSantoCecoop_114 = 138,
    [BrazilianBankMetadata("Central De Cooperativas De Economia E Credito Mutuo Do Estado Do Rio Grande Do S", "091", "1634601")]
    CentralDeCooperativasDeEconomiaECreditoMutuoDoEstadoDoRioGra_091 = 139,
    [BrazilianBankMetadata("China Construction Bank (Brasil) Banco Multiplo S.A.", "320", "7450604")]
    ChinaConstructionBankBrasilBancoMultiploSA_320 = 140,
    [BrazilianBankMetadata("Cielo S.A.", "362", "1027058")]
    CieloSA_362 = 141,
    [BrazilianBankMetadata("Citibank N.A.", "477", "33042953")]
    CitibankNA_477 = 142,
    [BrazilianBankMetadata("Cm Capital Markets Corretora De Cambio, Titulos E Valores Mobiliarios Ltda", "180", "2685483")]
    CmCapitalMarketsCorretoraDeCambioTitulosEValoresMobiliariosL_180 = 143,
    [BrazilianBankMetadata("Codepe Corretora De Valores E Cambio S.A.", "127", "9512542")]
    CodepeCorretoraDeValoresECambioSA_127 = 144,
    [BrazilianBankMetadata("Commerzbank Brasil S.A. - Banco Multiplo", "163", "23522214")]
    CommerzbankBrasilSABancoMultiplo_163 = 145,
    [BrazilianBankMetadata("Banco Cresol", "133", "10398952")]
    BancoCresol_133 = 146,
    [BrazilianBankMetadata("Unicred Do Brasil", "136", "315557")]
    UnicredDoBrasil_136 = 147,
    [BrazilianBankMetadata("Confidence Corretora De Cambio S.A.", "060", "4913129")]
    ConfidenceCorretoraDeCambioSA_060 = 148,
    [BrazilianBankMetadata("Cooperativa Central De Credito - Ailos", "085", "5463212")]
    CooperativaCentralDeCreditoAilos_085 = 149,
    [BrazilianBankMetadata("Cooperativa de Credito Mutuo dos Despachantes de Transito", "016", "4715685")]
    CooperativadeCreditoMutuodosDespachantesdeTransito_016 = 150,
    [BrazilianBankMetadata("Cooperativa De Credito Rural Coopavel", "281", "76461557")]
    CooperativaDeCreditoRuralCoopavel_281 = 151,
    [BrazilianBankMetadata("Cooperativa De Credito Rural De Abelardo Luz - Sulcredi/Crediluz", "322", "1073966")]
    CooperativaDeCreditoRuralDeAbelardoLuzSulcrediCrediluz_322 = 152,
    [BrazilianBankMetadata("Cooperativa De Credito Rural De Ibiam - Sulcredi/Ibiam", "391", "8240446")]
    CooperativaDeCreditoRuralDeIbiamSulcrediIbiam_391 = 153,
    [BrazilianBankMetadata("Cooperativa De Credito Rural De Ouro Sulcredi/Ouro", "286", "7853842")]
    CooperativaDeCreditoRuralDeOuroSulcrediOuro_286 = 154,
    [BrazilianBankMetadata("Cooperativa De Credito Rural De Primavera Do Leste", "279", "26563270")]
    CooperativaDeCreditoRuralDePrimaveraDoLeste_279 = 155,
    [BrazilianBankMetadata("Cooperativa De Credito Rural De Sao Miguel Do Oeste - Sulcredi/Sao Miguel", "273", "8253539")]
    CooperativaDeCreditoRuralDeSaoMiguelDoOesteSulcrediSaoMiguel_273 = 156,
    [BrazilianBankMetadata("Cora Sociedade De Credito Direto S.A.", "403", "37880206")]
    CoraSociedadeDeCreditoDiretoSA_403 = 157,
    [BrazilianBankMetadata("Credialianca Cooperativa De Credito Rural", "098", "78157146")]
    CredialiancaCooperativaDeCreditoRural_098 = 158,
    [BrazilianBankMetadata("Credicoamo Credito Rural Cooperativa", "010", "81723108")]
    CredicoamoCreditoRuralCooperativa_010 = 159,
    [BrazilianBankMetadata("Credisan Cooperativa De Credito", "089", "62109566")]
    CredisanCooperativaDeCredito_089 = 160,
    [BrazilianBankMetadata("Credisis - Central De Cooperativas De Credito Ltda.", "097", "4632856")]
    CredisisCentralDeCooperativasDeCreditoLtda_097 = 161,
    [BrazilianBankMetadata("Credit Suisse Hedging-Griffo Corretora De Valores S.A", "011", "61809182")]
    CreditSuisseHedgingGriffoCorretoraDeValoresSA_011 = 162,
    [BrazilianBankMetadata("Creditas Sociedade De Credito Direto S.A.", "342", "32997490")]
    CreditasSociedadeDeCreditoDiretoSA_342 = 163,
    [BrazilianBankMetadata("Crefaz Sociedade De Credito Ao Microempreendedor E A Empresa De Pequeno Porte Lt", "321", "18188384")]
    CrefazSociedadeDeCreditoAoMicroempreendedorEAEmpresaDePequen_321 = 164,
    [BrazilianBankMetadata("Decyseo Corretora De Cambio Ltda.", "289", "94968518")]
    DecyseoCorretoraDeCambioLtda_289 = 165,
    [BrazilianBankMetadata("Deutsche Bank S.A. - Banco Alemao", "487", "62331228")]
    DeutscheBankSABancoAlemao_487 = 166,
    [BrazilianBankMetadata("Easynvest - Titulo Corretora De Valores Sa", "140", "62169875")]
    EasynvestTituloCorretoraDeValoresSa_140 = 167,
    [BrazilianBankMetadata("Facta Financeira S.A.", "149", "15581638")]
    FactaFinanceiraSA_149 = 168,
    [BrazilianBankMetadata("Fair Corretora De Cambio S.A.", "196", "32648370")]
    FairCorretoraDeCambioSA_196 = 169,
    [BrazilianBankMetadata("Ffa Sociedade De Credito Ao Microempreendedor E A Empresa De Pequeno Porte Ltda.", "343", "24537861")]
    FfaSociedadeDeCreditoAoMicroempreendedorEAEmpresaDePequenoPo_343 = 170,
    [BrazilianBankMetadata("Fram Capital Distribuidora De Titulos E Valores Mobiliarios S.A.", "331", "13673855")]
    FramCapitalDistribuidoraDeTitulosEValoresMobiliariosSA_331 = 171,
    [BrazilianBankMetadata("Frente Corretora De Cambio Ltda.", "285", "71677850")]
    FrenteCorretoraDeCambioLtda_285 = 172,
    [BrazilianBankMetadata("Genial Investimentos Corretora De Valores Mobiliarios S.A.", "278", "27652684")]
    GenialInvestimentosCorretoraDeValoresMobiliariosSA_278 = 173,
    [BrazilianBankMetadata("Gerencianet S.A.", "364", "9089356")]
    GerencianetSA_364 = 174,
    [BrazilianBankMetadata("Get Money Corretora De Cambio S.A.", "138", "10853017")]
    GetMoneyCorretoraDeCambioSA_138 = 175,
    [BrazilianBankMetadata("Global Financas", "384", "11165756")]
    GlobalFinancas_384 = 176,
    [BrazilianBankMetadata("Goldman Sachs Do Brasil Banco Multiplo S.A.", "064", "4332281")]
    GoldmanSachsDoBrasilBancoMultiploSA_064 = 177,
    [BrazilianBankMetadata("Guide Investimentos S.A. Corretora De Valores", "177", "65913436")]
    GuideInvestimentosSACorretoraDeValores_177 = 178,
    [BrazilianBankMetadata("Guitta Corretora De Cambio Ltda.", "146", "24074692")]
    GuittaCorretoraDeCambioLtda_146 = 179,
    [BrazilianBankMetadata("Haitong Banco De Investimento Do Brasil S.A.", "078", "34111187")]
    HaitongBancoDeInvestimentoDoBrasilSA_078 = 180,
    [BrazilianBankMetadata("Hipercard Banco Multiplo S.A.", "062", "3012230")]
    HipercardBancoMultiploSA_062 = 181,
    [BrazilianBankMetadata("HS Financeira S/A Credito", "189", "7512441")]
    HSFinanceiraSACredito_189 = 182,
    [BrazilianBankMetadata("Hub Pagamentos S.A", "396", "13884775")]
    HubPagamentosSA_396 = 183,
    [BrazilianBankMetadata("Ib Corretora De Cambio, Titulos E Valores Mobiliarios S.A.", "271", "27842177")]
    IbCorretoraDeCambioTitulosEValoresMobiliariosSA_271 = 184,
    [BrazilianBankMetadata("Icap Do Brasil Corretora De Titulos E Valores Mobiliarios Ltda.", "157", "9105360")]
    IcapDoBrasilCorretoraDeTitulosEValoresMobiliariosLtda_157 = 185,
    [BrazilianBankMetadata("Icbc Do Brasil Banco Multiplo S.A.", "132", "17453575")]
    IcbcDoBrasilBancoMultiploSA_132 = 186,
    [BrazilianBankMetadata("Ing Bank N.V.", "492", "49336860")]
    IngBankNV_492 = 187,
    [BrazilianBankMetadata("Intesa Sanpaolo Brasil S.A. - Banco Multiplo", "139", "55230916")]
    IntesaSanpaoloBrasilSABancoMultiplo_139 = 188,
    [BrazilianBankMetadata("Itau Unibanco Holding S.A.", "652", "60872504")]
    ItauUnibancoHoldingSA_652 = 189,
    [BrazilianBankMetadata("Itau Unibanco S.A.", "341", "60701190")]
    ItauUnibancoSA_341 = 190,
    [BrazilianBankMetadata("Jpmorgan Chase Bank", "488", "46518205")]
    JpmorganChaseBank_488 = 191,
    [BrazilianBankMetadata("Kirton Bank S.A. - Banco Multiplo", "399", "1701201")]
    KirtonBankSABancoMultiplo_399 = 192,
    [BrazilianBankMetadata("Lastro Rdv Distribuidora De Titulos E Valores Mobiliarios Ltda.", "293", "71590442")]
    LastroRdvDistribuidoraDeTitulosEValoresMobiliariosLtda_293 = 193,
    [BrazilianBankMetadata("Lecca Credito", "105", "7652226")]
    LeccaCredito_105 = 194,
    [BrazilianBankMetadata("Levycam - Corretora De Cambio E Valores Ltda.", "145", "50579044")]
    LevycamCorretoraDeCambioEValoresLtda_145 = 195,
    [BrazilianBankMetadata("Listo Sociedade De Credito Direto S.A.", "397", "34088029")]
    ListoSociedadeDeCreditoDiretoSA_397 = 196,
    [BrazilianBankMetadata("Magliano S.A. Corretora De Cambio E Valores Mobiliarios", "113", "61723847")]
    MaglianoSACorretoraDeCambioEValoresMobiliarios_113 = 197,
    [BrazilianBankMetadata("Mercadopago.Com Representacoes Ltda.", "323", "10573521")]
    MercadopagoComRepresentacoesLtda_323 = 198,
    [BrazilianBankMetadata("Money Plus Sociedade De Credito", "274", "11581339")]
    MoneyPlusSociedadeDeCredito_274 = 199,
    [BrazilianBankMetadata("Moneycorp Banco De Cambio S.A.", "259", "8609934")]
    MoneycorpBancoDeCambioSA_259 = 200,
    [BrazilianBankMetadata("Ms Bank S.A. Banco De Cambio", "128", "19307785")]
    MsBankSABancoDeCambio_128 = 201,
    [BrazilianBankMetadata("Necton Investimentos S.A.", "354", "52904364")]
    NectonInvestimentosSA_354 = 202,
    [BrazilianBankMetadata("Nova Futura Corretora de Titulos e Valores Mobiliarios Ltda.", "191", "4257795")]
    NovaFuturaCorretoradeTituloseValoresMobiliariosLtda_191 = 203,
    [BrazilianBankMetadata("Novo Banco Continental S.A. - Banco Multiplo", "753", "74828799")]
    NovoBancoContinentalSABancoMultiplo_753 = 204,
    [BrazilianBankMetadata("Nu Pagamentos S.A.", "260", "18236120")]
    NuPagamentosSA_260 = 205,
    [BrazilianBankMetadata("Oliveira Trust Distribuidora de Titulos e Valores Mobiliarios S.A.", "111", "36113876")]
    OliveiraTrustDistribuidoradeTituloseValoresMobiliariosSA_111 = 206,
    [BrazilianBankMetadata("Om Distribuidora de Titulos e Valores Mobiliarios Ltda", "319", "11495073")]
    OmDistribuidoradeTituloseValoresMobiliariosLtda_319 = 207,
    [BrazilianBankMetadata("Omni Banco S.A.", "613", "60850229")]
    OmniBancoSA_613 = 208,
    [BrazilianBankMetadata("Orama Distribuidora de Titulos e Valores Mobiliarios S.A.", "325", "13293225")]
    OramaDistribuidoradeTituloseValoresMobiliariosSA_325 = 209,
    [BrazilianBankMetadata("Otimo Sociedade de Credito Direto S.A.", "355", "34335592")]
    OtimoSociedadedeCreditoDiretoSA_355 = 210,
    [BrazilianBankMetadata("Pagseguro Internet S.A.", "290", "8561701")]
    PagseguroInternetSA_290 = 211,
    [BrazilianBankMetadata("Parana Banco S.A.", "254", "14388334")]
    ParanaBancoSA_254 = 212,
    [BrazilianBankMetadata("Parati - Credito", "326", "3311443")]
    ParatiCredito_326 = 213,
    [BrazilianBankMetadata("Parmetal Distribuidora de Titulos e Valores Mobiliarios Ltda", "194", "20155248")]
    ParmetalDistribuidoradeTituloseValoresMobiliariosLtda_194 = 214,
    [BrazilianBankMetadata("Pefisa S.A. - Credito", "174", "43180355")]
    PefisaSACredito_174 = 215,
    [BrazilianBankMetadata("Pi Distribuidora de Titulos e Valores Mobiliarios S.A.", "315", "3502968")]
    PiDistribuidoradeTituloseValoresMobiliariosSA_315 = 216,
    [BrazilianBankMetadata("Picpay Servicos S.A.", "380", "22896431")]
    PicpayServicosSA_380 = 217,
    [BrazilianBankMetadata("Planner Corretora de Valores S.A.", "100", "806535")]
    PlannerCorretoradeValoresSA_100 = 218,
    [BrazilianBankMetadata("Plural S.A. Banco Multiplo", "125", "45246410")]
    PluralSABancoMultiplo_125 = 219,
    [BrazilianBankMetadata("Portocred S.A. - Credito", "108", "1800019")]
    PortocredSACredito_108 = 220,
    [BrazilianBankMetadata("Portopar Distribuidora de Titulos e Valores Mobiliarios Ltda.", "306", "40303299")]
    PortoparDistribuidoradeTituloseValoresMobiliariosLtda_306 = 221,
    [BrazilianBankMetadata("Qi Sociedade de Credito Direto S.A. 306", "306", "40303299")]
    QiSociedadedeCreditoDiretoSA306_306 = 222,
    [BrazilianBankMetadata("Qi Sociedade de Credito Direto S.A. 329", "329", "32402502")]
    QiSociedadedeCreditoDiretoSA329_329 = 223,
    [BrazilianBankMetadata("Rb Capital Investimentos Distribuidora", "283", "89960090")]
    RbCapitalInvestimentosDistribuidora_283 = 224,
    [BrazilianBankMetadata("Realize Credito", "374", "27351731")]
    RealizeCredito_374 = 225,
    [BrazilianBankMetadata("Renascenca Distribuidora de Titulos e Valores Mobiliarios Ltda", "101", "62287735")]
    RenascencaDistribuidoradeTituloseValoresMobiliariosLtda_101 = 226,
    [BrazilianBankMetadata("Sagitur Corretora De Cambio Ltda.", "270", "61444949")]
    SagiturCorretoraDeCambioLtda_270 = 227,
    [BrazilianBankMetadata("Scotiabank Brasil S.A. Banco Multiplo", "751", "29030467")]
    ScotiabankBrasilSABancoMultiplo_751 = 228,
    [BrazilianBankMetadata("Senff S.A. - Credito", "276", "11970623")]
    SenffSACredito_276 = 229,
    [BrazilianBankMetadata("Senso Corretora De Cambio E Valores Mobiliarios S.A", "545", "17352220")]
    SensoCorretoraDeCambioEValoresMobiliariosSA_545 = 230,
    [BrazilianBankMetadata("Servicoop - Cooperativa De Credito", "190", "3973814")]
    ServicoopCooperativaDeCredito_190 = 231,
    [BrazilianBankMetadata("Socopa Sociedade Corretora Paulista S.A.", "363", "62285390")]
    SocopaSociedadeCorretoraPaulistaSA_363 = 232,
    [BrazilianBankMetadata("Socred S.A.", "183", "9210106")]
    SocredSA_183 = 233,
    [BrazilianBankMetadata("Solidus S.A. Corretora de Cambio e Valores Mobiliarios", "365", "68757681")]
    SolidusSACorretoradeCambioeValoresMobiliarios_365 = 234,
    [BrazilianBankMetadata("Sorocred Credito", "299", "4814563")]
    SorocredCredito_299 = 235,
    [BrazilianBankMetadata("State Street Brasil S.A. - Banco Comercial", "014", "9274232")]
    StateStreetBrasilSABancoComercial_014 = 236,
    [BrazilianBankMetadata("Stone Pagamentos S.A.", "197", "16501555")]
    StonePagamentosSA_197 = 237,
    [BrazilianBankMetadata("Sumup Sociedade De Credito Direto S.A.", "404", "37241230")]
    SumupSociedadeDeCreditoDiretoSA_404 = 238,
    [BrazilianBankMetadata("Super Pagamentos e Administracao de Meios Eletronicos S.A.", "340", "9554480")]
    SuperPagamentoseAdministracaodeMeiosEletronicosSA_340 = 239,
    [BrazilianBankMetadata("Terra Investimentos Distribuidora", "370", "3751794")]
    TerraInvestimentosDistribuidora_370 = 240,
    [BrazilianBankMetadata("Toro Corretora De Titulos E Valores Mobiliarios Ltda", "352", "29162769")]
    ToroCorretoraDeTitulosEValoresMobiliariosLtda_352 = 241,
    [BrazilianBankMetadata("Travelex Banco De Cambio S.A.", "095", "11703662")]
    TravelexBancoDeCambioSA_095 = 242,
    [BrazilianBankMetadata("Treviso Corretora De Cambio S.A.", "143", "2992317")]
    TrevisoCorretoraDeCambioSA_143 = 243,
    [BrazilianBankMetadata("Tullett Prebon Brasil Corretora de Valores e Cambio Ltda", "131", "61747085")]
    TullettPrebonBrasilCorretoradeValoreseCambioLtda_131 = 244,
    [BrazilianBankMetadata("Ubs Brasil Banco de Investimento S.A.", "129", "18520834")]
    UbsBrasilBancodeInvestimentoSA_129 = 245,
    [BrazilianBankMetadata("Ubs Brasil Corretora de Cambio, Titulos e Valores Mobiliarios S.A.", "015", "2819125")]
    UbsBrasilCorretoradeCambioTituloseValoresMobiliariosSA_015 = 246,
    [BrazilianBankMetadata("Uniprime Central", "099", "3046391")]
    UniprimeCentral_099 = 247,
    [BrazilianBankMetadata("Uniprime Norte Do Parana", "084", "2398976")]
    UniprimeNorteDoParana_084 = 248,
    [BrazilianBankMetadata("UP.P Sociedade de Emprestimo Entre Pessoas S.A.", "373", "35977097")]
    UPPSociedadedeEmprestimoEntrePessoasSA_373 = 249,
    [BrazilianBankMetadata("Vips Corretora de Cambio Ltda.", "298", "17772370")]
    VipsCorretoradeCambioLtda_298 = 250,
    [BrazilianBankMetadata("Vision S.A. Corretora De Cambio", "296", "4062902")]
    VisionSACorretoraDeCambio_296 = 251,
    [BrazilianBankMetadata("Vitreo Distribuidora de Titulos e Valores Mobiliarios S.A.", "367", "34711571")]
    VitreoDistribuidoradeTituloseValoresMobiliariosSA_367 = 252,
    [BrazilianBankMetadata("Vortx Distribuidora de Titulos e Valores Mobiliarios Ltda.", "310", "22610500")]
    VortxDistribuidoradeTituloseValoresMobiliariosLtda_310 = 253,
    [BrazilianBankMetadata("Xp Investimentos Corretora de Cambio", "102", "2332886")]
    XpInvestimentosCorretoradeCambio_102 = 254,
    [BrazilianBankMetadata("Zema Credito", "359", "5351887")]
    ZemaCredito_359 = 255,
}

public static class BrazilianBankMetadata
{
    public static (string Name, string Code, string Ispb) Get(BrazilianBank bank)
    {
        var member = typeof(BrazilianBank).GetMember(bank.ToString())[0];
        var attr = member.GetCustomAttributes(typeof(BrazilianBankMetadataAttribute), false)
            .Cast<BrazilianBankMetadataAttribute>()
            .Single();
        return (attr.Name, attr.Code, attr.Ispb);
    }
}
