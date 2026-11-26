import { describe, expect, it } from 'vitest';
import { ErroApi } from '@/types/api';
import { desfechoFalhaDoEnvio, desfechoFalhaDoPost, passoDoEnvio } from './sequenciaEnvioAvaria';

/** 502 do POST: a avaria foi gravada e a foto não. O corpo traz idAvaria e retryUploadUrl (contrato avaria). */
function erro502ComId(idAvaria: number): ErroApi {
  return new ErroApi({
    status: 502,
    mensagem: 'A avaria foi criada, mas o upload da foto falhou. Repita o envio pela rota informada.',
    problema: { status: 502, idAvaria, retryUploadUrl: `/api/vistorias/2/avarias/${idAvaria}/foto` },
  });
}

describe('passoDoEnvio', () => {
  it('item sem id cria a avaria (POST)', () => {
    expect(passoDoEnvio(null)).toEqual({ tipo: 'criar_avaria' });
  });

  it('item com id nunca cria de novo: só envia a foto (PUT)', () => {
    expect(passoDoEnvio(7)).toEqual({ tipo: 'enviar_foto', idAvaria: 7 });
  });
});

describe('desfechoFalhaDoPost', () => {
  it('502 com idAvaria no corpo: a avaria existe e falta a foto', () => {
    expect(desfechoFalhaDoPost(erro502ComId(7))).toEqual({ tipo: 'foto_pendente', idAvaria: 7 });
  });

  it('502 sem idAvaria no corpo: o hook procura a avaria antes de qualquer nova tentativa', () => {
    const semId = new ErroApi({ status: 502, mensagem: 'Falha.' });
    expect(desfechoFalhaDoPost(semId)).toEqual({ tipo: 'sem_id_no_502' });
  });

  it.each([400, 404, 409, 500])('status %i não é falha de foto: erro', (status) => {
    expect(desfechoFalhaDoPost(new ErroApi({ status, mensagem: 'x' }))).toEqual({ tipo: 'erro' });
  });

  it('erro que não veio da API é erro', () => {
    expect(desfechoFalhaDoPost(new Error('rede'))).toEqual({ tipo: 'erro' });
  });
});

describe('desfechoFalhaDoEnvio: decisão pura de cada falha (o enviarUm do hook não tem teste automatizado)', () => {
  it('POST com 502 grava a avaria; o reenvio usa só o PUT, sem POST de novo', () => {
    const desfecho = desfechoFalhaDoEnvio(null, erro502ComId(7));
    expect(desfecho).toEqual({ tipo: 'foto_pendente', idAvaria: 7 });
    // O item passa a ter id: o próximo passo é só o PUT da foto.
    expect(passoDoEnvio(desfecho.tipo === 'foto_pendente' ? desfecho.idAvaria : null)).toEqual({ tipo: 'enviar_foto', idAvaria: 7 });
  });

  it('PUT da foto falha: a avaria já tem id e continua pendente da foto, nunca volta a criar', () => {
    const desfecho = desfechoFalhaDoEnvio(7, new ErroApi({ status: 502, mensagem: 'x' }));
    expect(desfecho).toEqual({ tipo: 'foto_pendente', idAvaria: 7 });
    expect(passoDoEnvio(7).tipo).toBe('enviar_foto');
  });

  it('POST que falha sem 502 segue como erro, e o usuário pode repetir o POST', () => {
    expect(desfechoFalhaDoEnvio(null, new ErroApi({ status: 409, mensagem: 'x' }))).toEqual({ tipo: 'erro' });
    expect(passoDoEnvio(null)).toEqual({ tipo: 'criar_avaria' });
  });
});
