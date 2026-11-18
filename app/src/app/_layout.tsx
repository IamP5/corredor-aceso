import { router, Stack } from 'expo-router';
import { Platform, Pressable, Text } from 'react-native';
import { SQLiteProvider } from 'expo-sqlite';
import { StatusBar } from 'expo-status-bar';
import { migrar, NOME_DO_BANCO } from '../banco/banco';
import { ProvedorDoCorredor } from '../estado/corredor';
import { VIDRO } from '../ui/plataforma';
import { COR, TAMANHO } from '../ui/tema';

// O botão padrão de voltar mede 30 pontos na web. Lá ele vira um de 48. No iPhone e no Android fica o nativo.
const voltarDaWeb =
  Platform.OS === 'web'
    ? {
        headerLeft: ({ canGoBack }: { canGoBack?: boolean }) =>
          canGoBack ? (
            <Pressable
              accessibilityRole="button"
              accessibilityLabel="Voltar"
              onPress={() => router.back()}
              hitSlop={8}
              style={{ minHeight: TAMANHO.toqueMinimo, minWidth: TAMANHO.toqueMinimo, justifyContent: 'center', paddingRight: 12 }}
            >
              <Text style={{ fontSize: TAMANHO.corpo, fontWeight: '700', color: COR.primario }}>‹ Voltar</Text>
            </Pressable>
          ) : null,
      }
    : {};

// No iPhone a barra de cima fica translúcida e o conteúdo passa por baixo dela, como nos apps do sistema.
// Cada ScrollView usa contentInsetAdjustmentBehavior="automatic" para começar abaixo da barra.
// No iOS 26 o sistema já desenha a barra e o botão de voltar em Liquid Glass: um desfoque nosso ficaria por cima dele.
const barraDoIos =
  Platform.OS !== 'ios'
    ? { headerStyle: { backgroundColor: COR.fundo } }
    : VIDRO
      ? { headerTransparent: true }
      : { headerTransparent: true, headerBlurEffect: 'systemChromeMaterial' as const };

export default function Layout() {
  return (
    <SQLiteProvider databaseName={NOME_DO_BANCO} onInit={migrar}>
      <ProvedorDoCorredor>
        <StatusBar style="dark" />
        <Stack
          screenOptions={{
            ...barraDoIos,
            headerShadowVisible: false,
            headerLargeStyle: VIDRO ? undefined : { backgroundColor: COR.fundo },
            headerLargeTitleShadowVisible: false,
            headerTitleStyle: {
              fontSize: Platform.OS === 'ios' ? TAMANHO.pequeno : TAMANHO.subtitulo,
              fontWeight: Platform.OS === 'ios' ? '600' : '500',
              color: COR.texto,
            },
            headerLargeTitleStyle: { color: COR.texto },
            headerTintColor: COR.primario,
            headerBackTitle: 'Voltar',
            ...voltarDaWeb,
            contentStyle: { backgroundColor: COR.fundo },
          }}
        >
          <Stack.Screen name="index" options={{ title: 'Entrar', headerShown: false }} />
          <Stack.Screen name="dia" options={{ headerLargeTitle: true, title: 'Hoje', headerBackVisible: false }} />
          <Stack.Screen name="radar" options={{ headerLargeTitle: true, title: 'Radar' }} />
          {/* O nome da pessoa já é o título da página. A barra fica só com o voltar, como no app Contatos. */}
          <Stack.Screen name="casa/[token]" options={{ title: '' }} />
          <Stack.Screen name="visita/[token]" options={{ title: 'Registrar visita' }} />
          <Stack.Screen name="mensagem" options={{ title: 'Mensagem para o grupo' }} />
          <Stack.Screen name="encerrar" options={{ title: 'Resumo do turno' }} />
        </Stack>
      </ProvedorDoCorredor>
    </SQLiteProvider>
  );
}
