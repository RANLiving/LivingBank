import { Capacitor } from '@capacitor/core';
import { NativeBiometric } from 'capacitor-native-biometric';

const CREDENTIALS_SERVER = 'livingbank.app';

export function isNativePlatform(): boolean {
  return Capacitor.isNativePlatform();
}

export async function isBiometricAvailable(): Promise<boolean> {
  if (!isNativePlatform()) return false;
  try {
    const result = await NativeBiometric.isAvailable();
    return result.isAvailable;
  } catch {
    return false;
  }
}

export async function saveBiometricCredentials(userName: string, password: string) {
  if (!isNativePlatform()) return;
  await NativeBiometric.setCredentials({
    username: userName,
    password,
    server: CREDENTIALS_SERVER,
  });
}

export async function loginWithBiometrics(): Promise<{ userName: string; password: string } | null> {
  if (!isNativePlatform()) return null;
  const available = await isBiometricAvailable();
  if (!available) return null;

  await NativeBiometric.verifyIdentity({
    reason: 'Entrar na LivingBank',
    title: 'Autenticação biométrica',
  });

  const credentials = await NativeBiometric.getCredentials({ server: CREDENTIALS_SERVER });
  return { userName: credentials.username, password: credentials.password };
}

export async function clearBiometricCredentials() {
  if (!isNativePlatform()) return;
  try {
    await NativeBiometric.deleteCredentials({ server: CREDENTIALS_SERVER });
  } catch {
    // sem credenciais guardadas
  }
}
