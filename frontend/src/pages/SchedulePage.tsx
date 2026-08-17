import { useEffect, useState, type FormEvent } from 'react';
import { api } from '../api/client';

interface ScheduleForm {
  time1: string;
  time2: string;
  time3: string;
  time4: string;
}

export default function SchedulePage() {
  const [form, setForm] = useState<ScheduleForm>({ time1: '06:00', time2: '12:00', time3: '18:00', time4: '23:00' });
  const [saved, setSaved] = useState(false);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    api.get('/api/sync/schedule').then(({ data }) => {
      setForm({
        time1: data.time1.slice(0, 5),
        time2: data.time2.slice(0, 5),
        time3: data.time3.slice(0, 5),
        time4: data.time4.slice(0, 5),
      });
    });
  }, []);

  async function handleSubmit(e: FormEvent) {
    e.preventDefault();
    setError(null);
    setSaved(false);
    try {
      await api.put('/api/sync/schedule', form);
      setSaved(true);
    } catch {
      setError('Falha ao guardar agendamento.');
    }
  }

  return (
    <div>
      <h1 style={{ fontSize: 24, marginBottom: 8 }}>Agendamento de leitura (Enable Banking)</h1>
      <p className="lb-muted" style={{ marginBottom: 16 }}>
        Define as 4 horas diárias (UTC) em que o sistema lê automaticamente os saldos e movimentos.
      </p>

      {error && <div className="lb-error-banner">{error}</div>}
      {saved && <div className="lb-card" style={{ borderColor: '#0a8a2e' }}>Agendamento guardado.</div>}

      <form onSubmit={handleSubmit} className="lb-card" style={{ display: 'grid', gridTemplateColumns: 'repeat(4, 1fr)', gap: 12, maxWidth: 480 }}>
        {(['time1', 'time2', 'time3', 'time4'] as const).map((key, i) => (
          <div className="lb-field" key={key}>
            <label>Hora {i + 1}</label>
            <input
              className="lb-input"
              type="time"
              value={form[key]}
              onChange={(e) => setForm({ ...form, [key]: e.target.value })}
              required
            />
          </div>
        ))}
        <div style={{ gridColumn: '1 / -1' }}>
          <button className="lb-btn" type="submit">Guardar</button>
        </div>
      </form>
    </div>
  );
}
