import { Stack } from 'expo-router';
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
            headerBackTitle: 'Voltar',
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
