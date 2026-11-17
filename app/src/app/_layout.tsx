import { router, Stack } from 'expo-router';
import { Pressable, Text } from 'react-native';
import { SQLiteProvider } from 'expo-sqlite';
import { StatusBar } from 'expo-status-bar';
import { migrar, NOME_DO_BANCO } from '../banco/banco';
import { ProvedorDoCorredor } from '../estado/corredor';
import { COR, TAMANHO } from '../ui/tema';

export default function Layout() {
  return (
    <SQLiteProvider databaseName={NOME_DO_BANCO} onInit={migrar}>
      <ProvedorDoCorredor>
        <StatusBar style="dark" />
        <Stack
          screenOptions={{
            headerTitleStyle: { fontSize: TAMANHO.subtitulo, fontWeight: '700', color: COR.texto },
            headerTintColor: COR.primario,
            // O botão padrão de voltar mede 30 pontos na web. Este mede 48 em todo lugar.
            headerLeft: ({ canGoBack }) =>
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
            contentStyle: { backgroundColor: COR.fundo },
          }}
        >
          <Stack.Screen name="index" options={{ title: 'Entrar' }} />
          <Stack.Screen name="dia" options={{ title: 'Dia de hoje', headerBackVisible: false }} />
          <Stack.Screen name="radar" options={{ title: 'Radar' }} />
          <Stack.Screen name="casa/[token]" options={{ title: 'Casa' }} />
          <Stack.Screen name="visita/[token]" options={{ title: 'Registrar visita' }} />
          <Stack.Screen name="mensagem" options={{ title: 'Mensagem para o grupo' }} />
          <Stack.Screen name="encerrar" options={{ title: 'Encerrar o turno' }} />
        </Stack>
      </ProvedorDoCorredor>
    </SQLiteProvider>
  );
}
