'use strict';
/* Glutherz – Der Schmied von Aschenfurt
   Ein eigenständiges Schmiede-RPG: Erz erkämpfen, Ausrüstung schmieden, Bosse besiegen. */

// ---------------------------------------------------------------- Daten
const RAR = ['Gewöhnlich', 'Selten', 'Episch', 'Einzigartig', 'Legendär'];
const RAR_MULT = [1, 1.3, 1.7, 2.3, 3.2];
const RAR_MIN = [0, 45, 65, 82, 100]; // nötiger Schmiedewurf je Seltenheit

const MATS = [
  { name: 'Eisenerz', adj: 'Eisen' },
  { name: 'Mondsilber', adj: 'Mondsilber' },
  { name: 'Frostkristall', adj: 'Frostkristall' },
  { name: 'Glutobsidian', adj: 'Obsidian' },
  { name: 'Sternenstahl', adj: 'Sternenstahl' },
];
const T_ATK = [8, 18, 34, 60, 100];
const T_DEF = [4, 9, 17, 30, 50];
const T_HP = [20, 50, 110, 220, 420];
const T_GOLD = [15, 50, 130, 300, 650];

const RECIPES = [
  { type: 'Schwert', slot: 'weapon', icon: '🗡️', ore: 5, atk: 1.0, speed: 1.0, desc: 'Ausgewogen' },
  { type: 'Axt', slot: 'weapon', icon: '🪓', ore: 5, atk: 1.4, speed: 0.75, desc: 'Wuchtig, aber langsam' },
  { type: 'Dolch', slot: 'weapon', icon: '🔪', ore: 5, atk: 0.7, speed: 1.45, crit: 10, desc: 'Schnell, +10 % Krit' },
  { type: 'Helm', slot: 'helm', icon: '⛑️', ore: 4, def: 0.6, hp: 0.5 },
  { type: 'Harnisch', slot: 'chest', icon: '🛡️', ore: 6, def: 1.0, hp: 1.0 },
  { type: 'Stiefel', slot: 'boots', icon: '🥾', ore: 3, def: 0.5, hp: 0.4 },
];
const SLOTS = { weapon: 'Waffe', helm: 'Helm', chest: 'Harnisch', boots: 'Stiefel' };

const AFFIX = {
  crit: { label: v => `+${v} % Kritische Treffer`, roll: () => rnd(3, 8) },
  ls: { label: v => `${v} % Lebensraub`, roll: () => rnd(2, 5) },
  hp: { label: v => `+${v} Leben`, roll: t => rnd(12, 20) * (t + 1) },
  atkp: { label: v => `+${v} % Angriff`, roll: () => rnd(4, 10) },
  def: { label: v => `+${v} Rüstung`, roll: t => rnd(2, 4) * (t + 1) },
  spd: { label: v => `+${v} % Angriffstempo`, roll: () => rnd(4, 10) },
  dbl: { label: v => `Doppelschlag: ${v} % Chance, zweimal zu treffen` },
};
const LEGEND_NAMES = ['Glutherz', 'Aschenbrand', 'Sturmzahn', 'Morgenrot', 'Nachtschwur', 'Eisenwille', 'Funkenlied', 'Donnergroll'];

const ZONES = [
  { name: 'Flüsterwald', mobs: [['Moosschleim', '🟢'], ['Grauwolf', '🐺']], boss: ['Der Alte Eber', '🐗'] },
  { name: 'Aschenmine', mobs: [['Höhlenfledermaus', '🦇'], ['Stollenkobold', '👺']], boss: ['Der Minenkönig', '👹'] },
  { name: 'Frostpass', mobs: [['Eiswolf', '🐺'], ['Schneegeist', '👻']], boss: ['Der Frostriese', '🧊'] },
  { name: 'Glutschlund', mobs: [['Feuerskorpion', '🦂'], ['Aschenechse', '🦎']], boss: ['Der Lavawurm', '🐉'] },
  { name: 'Sternenruine', mobs: [['Runenwächter', '🗿'], ['Sternenschatten', '👤']], boss: ['Der Gefallene Wächter', '😈'] },
];
const Z_HP = [40, 140, 380, 900, 2000];
const Z_ATK = [6, 16, 34, 65, 120];
const Z_DEF = [0, 4, 12, 26, 50];
const Z_XP = [8, 25, 60, 140, 320];
const Z_GOLD = [5, 14, 35, 80, 180];
const BOSS_KILLS = 5;

// ---------------------------------------------------------------- Helfer
const $ = s => document.querySelector(s);
function rnd(a, b) { return Math.floor(a + Math.random() * (b - a + 1)); }
function pick(a) { return a[Math.floor(Math.random() * a.length)]; }
function clamp(v, a, b) { return Math.max(a, Math.min(b, v)); }
function toast(html) {
  const d = document.createElement('div');
  d.className = 'toast'; d.innerHTML = html;
  $('#toasts').appendChild(d);
  setTimeout(() => d.remove(), 3300);
}

// ---------------------------------------------------------------- Spielstand
const SAVE_KEY = 'glutherz-save-v1';
let S;
function newGame() {
  S = {
    lvl: 1, xp: 0, pts: 0, str: 0, vit: 0, agi: 0, gold: 30, potions: 2,
    smith: { lvl: 1, xp: 0 }, mats: [6, 0, 0, 0, 0], inv: [], eq: {},
    zones: 1, kills: [0, 0, 0, 0, 0], bosses: [0, 0, 0, 0, 0], nextId: 1, won: false,
  };
  const starter = { id: S.nextId++, slot: 'weapon', type: 'Schwert', icon: '🗡️', tier: 0, rar: 0, q: 0, plus: 0,
    atk: 4, def: 0, hp: 0, speed: 1, aff: [], name: 'Rostiges Schwert', value: 2 };
  S.eq.weapon = starter;
}
function save() { try { localStorage.setItem(SAVE_KEY, JSON.stringify(S)); } catch (e) { /* Speichern nicht möglich */ } }
function load() {
  try {
    const raw = localStorage.getItem(SAVE_KEY);
    if (raw) { S = JSON.parse(raw); return; }
  } catch (e) { /* defekter Spielstand -> neu */ }
  newGame();
}

function xpNeed(l) { return Math.round(30 * Math.pow(l, 1.5)); }
function smithNeed(l) { return 40 * l; }
function plusMult(it) { return 1 + 0.08 * it.plus; }

function stats() {
  const st = { atk: 4 + S.lvl + S.str * 2, def: 0, hp: 90 + S.lvl * 10 + S.vit * 12, crit: 5 + S.agi, spd: S.agi * 1.5, ls: 0, dbl: 0, atkp: 0, wspeed: 1 };
  for (const slot in S.eq) {
    const it = S.eq[slot]; if (!it) continue;
    const m = plusMult(it);
    st.atk += it.atk * m; st.def += it.def * m; st.hp += it.hp * m;
    if (slot === 'weapon') st.wspeed = it.speed;
    for (const a of it.aff) {
      if (a.k === 'hp') st.hp += a.v; else if (a.k === 'def') st.def += a.v; else st[a.k] += a.v;
    }
  }
  st.atk = Math.round(st.atk * (1 + st.atkp / 100));
  st.def = Math.round(st.def); st.hp = Math.round(st.hp);
  st.crit = Math.min(75, st.crit); st.ls = Math.min(40, st.ls);
  st.interval = 1.6 / (st.wspeed * (1 + st.spd / 100)); // Sekunden pro Schlag
  return st;
}

function gainXp(n) {
  S.xp += n;
  while (S.xp >= xpNeed(S.lvl)) {
    S.xp -= xpNeed(S.lvl); S.lvl++; S.pts += 3;
    toast(`⭐ <b>Stufe ${S.lvl}!</b> +3 Attributspunkte`);
  }
}
function gainSmithXp(n) {
  if (S.smith.lvl >= 25) return;
  S.smith.xp += n;
  while (S.smith.lvl < 25 && S.smith.xp >= smithNeed(S.smith.lvl)) {
    S.smith.xp -= smithNeed(S.smith.lvl); S.smith.lvl++;
    toast(`🔨 <b>Schmiedekunst Stufe ${S.smith.lvl}!</b>`);
  }
}

// ---------------------------------------------------------------- Gegenstände
function makeItem(rec, tier, rar, q) {
  const qm = 0.9 + q / 500; // Qualität 0..100 -> 0.9..1.1
  const m = RAR_MULT[rar] * qm;
  const it = {
    id: S.nextId++, slot: rec.slot, type: rec.type, icon: rec.icon, tier, rar, q: Math.round(q), plus: 0,
    atk: Math.round(T_ATK[tier] * (rec.atk || 0) * m),
    def: Math.round(T_DEF[tier] * (rec.def || 0) * m),
    hp: Math.round(T_HP[tier] * (rec.hp || 0) * m),
    speed: rec.speed || 1, aff: [], value: Math.round(T_GOLD[tier] * 0.4 * (rar + 1)),
  };
  if (rec.crit) it.aff.push({ k: 'crit', v: rec.crit });
  const pool = (rec.slot === 'weapon' ? ['crit', 'ls', 'atkp', 'spd'] : ['hp', 'def', 'crit', 'spd', 'atkp'])
    .filter(k => !it.aff.some(a => a.k === k));
  for (let i = 0; i < rar && pool.length; i++) {
    const k = pool.splice(rnd(0, pool.length - 1), 1)[0];
    it.aff.push({ k, v: AFFIX[k].roll(tier) });
  }
  const base = `${MATS[tier].adj}-${rec.type}`;
  if (rar === 4) { it.aff.push({ k: 'dbl', v: 20 }); it.name = `${pick(LEGEND_NAMES)}, ${base}`; }
  else it.name = base;
  return it;
}
function itemHtml(it, buttons) {
  const m = plusMult(it), lines = [];
  if (it.atk) lines.push(`Angriff ${Math.round(it.atk * m)}`);
  if (it.slot === 'weapon') lines.push(`Tempo ×${it.speed}`);
  if (it.def) lines.push(`Rüstung ${Math.round(it.def * m)}`);
  if (it.hp) lines.push(`Leben +${Math.round(it.hp * m)}`);
  for (const a of it.aff) lines.push(AFFIX[a.k].label(a.v));
  return `<div class="card item b${it.rar}">
    <div class="row between"><span class="name r${it.rar}">${it.icon} ${it.name}${it.plus ? ' +' + it.plus : ''}</span>
    <span class="tag">${RAR[it.rar]}</span></div>
    <ul>${lines.map(l => `<li>${l}</li>`).join('')}</ul>
    ${buttons ? `<div class="btns">${buttons}</div>` : ''}</div>`;
}
function findItem(id) {
  for (const slot in S.eq) if (S.eq[slot] && S.eq[slot].id === id) return S.eq[slot];
  return S.inv.find(i => i.id === id);
}
function enhanceCost(it) { return { gold: Math.round(T_GOLD[it.tier] * 0.3 * (it.plus + 1)), ore: 1 + Math.floor(it.plus / 3) }; }
function enhanceChance(it) { return Math.max(15, 100 - it.plus * 10); }

// ---------------------------------------------------------------- Oberfläche
let tab = 'adventure', confirmReset = false;
const TABS = [['adventure', '⚔️ Abenteuer'], ['forge', '🔨 Schmiede'], ['inv', '🎒 Inventar'], ['char', '🧍 Charakter'], ['shop', '🧪 Händler']];

function renderHud() {
  const st = stats();
  $('#hud').innerHTML = `<h1><span>Glutherz</span> – Der Schmied von Aschenfurt</h1>
    <div class="hudrow"><span>Stufe <b>${S.lvl}</b></span><span>🪙 <b>${S.gold}</b></span><span>🧪 <b>${S.potions}</b></span>
    <span>🔨 Schmiedekunst <b>${S.smith.lvl}</b></span><span>⚔️ <b>${st.atk}</b></span><span>🛡️ <b>${st.def}</b></span><span>❤️ <b>${st.hp}</b></span>
    ${S.pts ? `<span class="r3">● ${S.pts} Punkte frei</span>` : ''}</div>
    <div class="bar xp" title="Erfahrung"><i style="width:${100 * S.xp / xpNeed(S.lvl)}%"></i></div>`;
  $('#nav').innerHTML = TABS.map(([k, l]) => `<button data-act="tab" data-a="${k}" class="${k === tab ? 'on' : ''}">${l}</button>`).join('');
}
function render() {
  renderHud();
  const v = $('#view');
  if (tab === 'adventure') v.innerHTML = C ? combatHtml() : zonesHtml();
  else if (tab === 'forge') v.innerHTML = F ? forgeGameHtml() : forgeHtml();
  else if (tab === 'inv') v.innerHTML = invHtml();
  else if (tab === 'char') v.innerHTML = charHtml();
  else v.innerHTML = shopHtml();
  if (C && tab === 'adventure') updateCombat();
  save();
}

// ---------------------------------------------------------------- Abenteuer & Kampf
let C = null, combatTimer = null, autoNext = false;

function zonesHtml() {
  return `<div class="panel"><h2>Wohin ziehst du?</h2>
    <p class="muted">Besiege ${BOSS_KILLS} Gegner, um den Boss eines Gebiets herauszufordern. Jedes Gebiet liefert ein besseres Erz für deine Schmiede.</p>
    <div class="grid">${ZONES.map((z, i) => {
      const open = i < S.zones, bossReady = S.kills[i] >= BOSS_KILLS;
      return `<div class="card ${open ? '' : 'locked'}"><h3>${z.boss[1]} ${z.name}</h3>
        <div class="muted">Beute: ${MATS[i].name} · Gegnerstärke ${i + 1}</div>
        <div class="muted">${open ? (S.bosses[i] ? `Boss besiegt (${S.bosses[i]}×)` : `Boss: ${Math.min(S.kills[i], BOSS_KILLS)}/${BOSS_KILLS} Siege`) : '🔒 Besiege den vorherigen Boss'}</div>
        <div class="btns"><button data-act="fight" data-a="${i}" ${open ? '' : 'disabled'}>Jagen</button>
        <button class="primary" data-act="boss" data-a="${i}" ${open && bossReady ? '' : 'disabled'}>Boss fordern</button></div></div>`;
    }).join('')}</div></div>
    ${S.won ? '<div class="panel"><b class="r4">Du hast den Gefallenen Wächter bezwungen!</b> Aschenfurt feiert seinen größten Schmied. Du kannst weiterspielen und nach der perfekten legendären Ausrüstung jagen.</div>' : ''}`;
}

function startFight(zone, boss) {
  const z = ZONES[zone], [name, icon] = boss ? z.boss : pick(z.mobs);
  const v = 0.85 + Math.random() * 0.3, st = stats();
  const keepHp = C && C.won ? C.hp : st.hp;
  C = {
    zone, boss, name, icon,
    emax: Math.round(Z_HP[zone] * v * (boss ? 5 : 1)), eatk: Math.round(Z_ATK[zone] * (boss ? 1.6 : 1)),
    edef: Math.round(Z_DEF[zone] * (boss ? 1.3 : 1)), eint: boss ? 1.7 : 2.0,
    hp: Math.min(keepHp, st.hp), pT: 0, eT: 0, cdSmash: 0, cdGuard: 0, cdPotion: 0, guard: 0, log: [], over: false, won: false,
  };
  C.ehp = C.emax;
  logC(`${icon} <b>${name}</b> stellt sich dir in den Weg!`);
  clearInterval(combatTimer);
  combatTimer = setInterval(tick, 50);
  render();
}
function logC(html, cls) {
  C.log.push(`<div class="${cls || ''}">${html}</div>`);
  if (C.log.length > 60) C.log.shift();
}
function hitFx(id) {
  const el = document.getElementById(id); if (!el) return;
  el.classList.add('hit'); setTimeout(() => el.classList.remove('hit'), 110);
}
function playerHit(mult, label) {
  const st = stats();
  const crit = Math.random() * 100 < st.crit;
  let dmg = Math.max(1, Math.round(st.atk * mult * (0.9 + Math.random() * 0.2) * (crit ? 1.8 : 1) - C.edef * 0.5));
  C.ehp = Math.max(0, C.ehp - dmg);
  if (st.ls) C.hp = Math.min(st.hp, C.hp + Math.ceil(dmg * st.ls / 100));
  logC(`${label || 'Du triffst'} für <b>${dmg}</b>${crit ? ' – Kritisch!' : ''}`, crit ? 'crit' : '');
  hitFx('esprite');
}
function tick() {
  if (!C || C.over) return;
  const dt = 0.05, st = stats();
  C.pT += dt; C.eT += dt;
  C.cdSmash = Math.max(0, C.cdSmash - dt); C.cdGuard = Math.max(0, C.cdGuard - dt);
  C.cdPotion = Math.max(0, C.cdPotion - dt); C.guard = Math.max(0, C.guard - dt);
  if (C.pT >= st.interval) {
    C.pT = 0; playerHit(1);
    if (C.ehp > 0 && Math.random() * 100 < st.dbl) playerHit(1, 'Doppelschlag! Du triffst');
  }
  if (C.ehp <= 0) return endFight(true);
  if (C.eT >= C.eint) {
    C.eT = 0;
    let dmg = Math.max(1, Math.round(C.eatk * (0.85 + Math.random() * 0.3) - st.def * 0.5));
    if (C.guard > 0) dmg = Math.ceil(dmg / 2);
    C.hp = Math.max(0, C.hp - dmg);
    logC(`${C.name} trifft dich für <b>${dmg}</b>${C.guard > 0 ? ' (abgeschwächt)' : ''}`, 'bad');
    hitFx('psprite');
  }
  if (C.hp <= 0) return endFight(false);
  updateCombat();
}
function endFight(won) {
  C.over = true; C.won = won;
  clearInterval(combatTimer);
  const z = C.zone;
  if (won) {
    const k = C.boss ? 8 : 1;
    const gold = Math.round(Z_GOLD[z] * k * (0.8 + Math.random() * 0.4));
    const ore = C.boss ? 8 : (Math.random() < 0.4 ? 2 : 1);
    S.gold += gold; S.mats[z] += ore; gainXp(Z_XP[z] * k);
    const maxHp = stats().hp; // kurze Verschnaufpause nach jedem Sieg
    C.hp = Math.min(maxHp, C.hp + Math.round(maxHp * 0.15));
    logC(`Sieg! +${Z_XP[z] * k} EP, +${gold} Gold, +${ore} ${MATS[z].name}`, 'good');
    if (C.boss) {
      S.bosses[z]++; S.kills[z] = 0;
      if (z + 1 < ZONES.length && S.zones < z + 2) { S.zones = z + 2; toast(`🗺️ Neues Gebiet: <b>${ZONES[z + 1].name}</b>`); }
      if (z === ZONES.length - 1 && !S.won) { S.won = true; toast('🏆 <b>Du hast das Spiel gemeistert!</b>'); }
    } else S.kills[z]++;
  } else {
    const lost = Math.floor(S.gold * 0.1);
    S.gold -= lost;
    logC(`Du wurdest besiegt und schleppst dich nach Aschenfurt zurück. −${lost} Gold`, 'bad');
  }
  render();
  if (won && autoNext && !C.boss) setTimeout(() => { if (C && C.over && C.won && tab === 'adventure') startFight(z, false); }, 900);
}
function leaveFight() { clearInterval(combatTimer); C = null; }

function combatHtml() {
  const z = ZONES[C.zone];
  return `<div class="panel"><div class="row between"><h2>${z.name}${C.boss ? ' – Bosskampf' : ''}</h2>
    <label class="muted"><input type="checkbox" data-act="auto" ${autoNext ? 'checked' : ''}> automatisch weiterkämpfen</label></div>
    <div class="arena">
      <div class="fighter"><div class="sprite" id="psprite">🧑‍🏭</div><b>Du</b>
        <div class="bar hp big"><i id="php"></i></div><div class="muted" id="phpt"></div><div class="bar atk"><i id="pat"></i></div></div>
      <div class="fighter"><div class="sprite" id="esprite">${C.icon}</div><b>${C.name}</b>
        <div class="bar hp big"><i id="ehp"></i></div><div class="muted" id="ehpt"></div><div class="bar atk"><i id="eat"></i></div></div>
    </div>
    <div class="btns" id="cbtns"></div>
    <div class="log" id="clog"></div></div>`;
}
function updateCombat() {
  if (!C || !$('#php')) return;
  const st = stats();
  $('#php').style.width = 100 * C.hp / st.hp + '%'; $('#phpt').textContent = `${C.hp} / ${st.hp}`;
  $('#ehp').style.width = 100 * C.ehp / C.emax + '%'; $('#ehpt').textContent = `${C.ehp} / ${C.emax}`;
  $('#pat').style.width = (C.over ? 0 : 100 * C.pT / st.interval) + '%';
  $('#eat').style.width = (C.over ? 0 : 100 * C.eT / C.eint) + '%';
  const cd = v => v > 0 ? ` (${v.toFixed(1)}s)` : '';
  const key = C.over ? 'over' : [C.cdSmash > 0, C.cdGuard > 0, C.cdPotion > 0, S.potions].join();
  const b = $('#cbtns');
  if (C.over) {
    if (b.dataset.key !== key) b.innerHTML = (C.won ? `<button class="primary" data-act="fight" data-a="${C.zone}">Weiterkämpfen</button>` : '') +
      `<button data-act="leave">Zurück nach Aschenfurt${C.won ? ' (heilt dich)' : ''}</button>`;
  } else if (b.dataset.key !== key) {
    b.innerHTML = `<button id="bs" data-act="smash"></button><button id="bg" data-act="guard"></button>
      <button id="bp" data-act="potion"></button><button data-act="leave">Fliehen</button>`;
  }
  b.dataset.key = key;
  if (!C.over) {
    $('#bs').textContent = `💥 Wuchtschlag${cd(C.cdSmash)}`; $('#bs').disabled = C.cdSmash > 0;
    $('#bg').textContent = `🛡️ Eisenhaut${cd(C.cdGuard)}`; $('#bg').disabled = C.cdGuard > 0;
    $('#bp').textContent = `🧪 Heiltrank ×${S.potions}${cd(C.cdPotion)}`; $('#bp').disabled = C.cdPotion > 0 || S.potions < 1;
  }
  const log = $('#clog');
  if (log.childElementCount !== C.log.length || C.log.length === 60) { log.innerHTML = C.log.join(''); log.scrollTop = log.scrollHeight; }
}

// ---------------------------------------------------------------- Schmiede
let F = null, forgeRaf = null, lastForged = null;
const STRIKES = 4;

function forgeHtml() {
  const last = lastForged ? `<div class="panel result"><div class="muted">Zuletzt geschmiedet (Qualität ${lastForged.q} %)</div>
    <div class="big r${lastForged.rar}">${RAR[lastForged.rar]}!</div>${itemHtml(lastForged)}</div>` : '';
  return `${last}<div class="panel"><h2>Schmiede von Aschenfurt</h2>
    <p class="muted">Triff beim Hämmern die glühende Zone – je genauer, desto höher die Qualität und die Chance auf seltene Stücke.
    Deine Schmiedekunst (Stufe ${S.smith.lvl}, ${S.smith.lvl >= 25 ? 'Maximum' : `${S.smith.xp}/${smithNeed(S.smith.lvl)} EP`}) verbessert jeden Wurf.</p>
    <div class="row">${MATS.map((m, i) => `<span class="tag">${m.name}: ${S.mats[i]}</span>`).join('')}</div></div>
    ${MATS.map((m, t) => t < S.zones ? `<div class="panel"><h3>${m.name} <span class="muted">(Vorrat ${S.mats[t]})</span></h3><div class="grid">
      ${RECIPES.map((r, ri) => {
        const ok = S.mats[t] >= r.ore && S.gold >= T_GOLD[t];
        const info = r.slot === 'weapon' ? `Angriff ~${Math.round(T_ATK[t] * r.atk)} · ${r.desc}` : `Rüstung ~${Math.round(T_DEF[t] * r.def)} · Leben ~${Math.round(T_HP[t] * r.hp)}`;
        return `<div class="card"><b>${r.icon} ${m.adj}-${r.type}</b><div class="muted">${info}</div>
          <div class="muted">Kosten: ${r.ore} ${m.name}, ${T_GOLD[t]} Gold</div>
          <div class="btns"><button class="primary small" data-act="forge" data-a="${ri}" data-b="${t}" ${ok ? '' : 'disabled'}>Schmieden</button></div></div>`;
      }).join('')}</div></div>` : '').join('')}`;
}
function startForge(ri, tier) {
  const rec = RECIPES[ri];
  if (S.mats[tier] < rec.ore || S.gold < T_GOLD[tier]) return;
  S.mats[tier] -= rec.ore; S.gold -= T_GOLD[tier];
  F = { rec, tier, scores: [], pos: 0, dir: 1, t0: performance.now(), done: false };
  newZone();
  render();
  cancelAnimationFrame(forgeRaf);
  forgeRaf = requestAnimationFrame(forgeFrame);
}
function newZone() {
  F.width = 0.17 - F.tier * 0.015;
  F.center = 0.2 + Math.random() * 0.6;
  F.speed = 0.75 + F.scores.length * 0.22 + F.tier * 0.08; // Durchläufe pro Sekunde
}
function forgeFrame(now) {
  if (!F || F.done) return;
  const dt = Math.min(0.05, (now - F.t0) / 1000); F.t0 = now;
  F.pos += F.dir * F.speed * dt;
  if (F.pos > 1) { F.pos = 2 - F.pos; F.dir = -1; }
  if (F.pos < 0) { F.pos = -F.pos; F.dir = 1; }
  const m = $('#marker'); if (m) m.style.left = F.pos * 100 + '%';
  forgeRaf = requestAnimationFrame(forgeFrame);
}
function strike() {
  if (!F || F.done) return;
  const d = Math.abs(F.pos - F.center), sweet = F.width * 0.18;
  const score = d <= sweet ? 100 : Math.max(0, Math.round(100 - (d - sweet) / 0.22 * 100));
  F.scores.push(score);
  if (F.scores.length >= STRIKES) return finishForge();
  newZone(); render();
}
function finishForge() {
  F.done = true; cancelAnimationFrame(forgeRaf);
  const q = F.scores.reduce((a, b) => a + b, 0) / STRIKES;
  const roll = q * 0.8 + S.smith.lvl * 1.2 + Math.random() * 20;
  let rar = 0;
  for (let i = 0; i < RAR_MIN.length; i++) if (roll >= RAR_MIN[i]) rar = i;
  const it = makeItem(F.rec, F.tier, rar, q);
  S.inv.push(it); lastForged = it;
  gainSmithXp(Math.round((10 + q / 5) * (F.tier + 1)));
  if (rar >= 3) toast(`✨ <b class="r${rar}">${RAR[rar]}!</b> ${it.name}`);
  F = null; render();
}
function forgeGameHtml() {
  const z = F.width, sw = z * 0.36;
  return `<div class="panel"><h2>${F.rec.icon} ${MATS[F.tier].adj}-${F.rec.type} schmieden</h2>
    <p class="muted">Schlag zu, wenn der weiße Strich in der hellen Mitte der Glutzone steht. (Leertaste oder Button)</p>
    <div class="anvil"><div class="zone" style="left:${(F.center - z / 2) * 100}%;width:${z * 100}%"></div>
      <div class="sweet" style="left:${(F.center - sw / 2) * 100}%;width:${sw * 100}%"></div>
      <div class="marker" id="marker" style="left:${F.pos * 100}%"></div></div>
    <div class="strikes">${Array.from({ length: STRIKES }, (_, i) => `<span>${F.scores[i] === undefined ? `Schlag ${i + 1}` : (F.scores[i] === 100 ? '🔥 Perfekt' : F.scores[i] + ' %')}</span>`).join('')}</div>
    <div class="btns"><button class="primary" data-act="strike">🔨 Zuschlagen</button></div></div>`;
}

// ---------------------------------------------------------------- Inventar, Charakter, Händler
function itemButtons(it, equipped) {
  const c = enhanceCost(it), can = it.plus < 10 && S.gold >= c.gold && S.mats[it.tier] >= c.ore;
  return (equipped ? '' : `<button class="small primary" data-act="equip" data-a="${it.id}">Ausrüsten</button>`) +
    (it.plus < 10 ? `<button class="small" data-act="enhance" data-a="${it.id}" ${can ? '' : 'disabled'}
      title="Bei Fehlschlag gehen nur die Materialien verloren">Verstärken ${enhanceChance(it)} % (${c.gold}🪙, ${c.ore} ${MATS[it.tier].name})</button>` : '<span class="tag">Maximal verstärkt</span>') +
    (equipped ? '' : `<button class="small" data-act="sell" data-a="${it.id}">Verkaufen ${it.value}🪙</button>`);
}
function invHtml() {
  const inv = [...S.inv].sort((a, b) => b.tier - a.tier || b.rar - a.rar);
  return `<div class="panel"><h2>Angelegt</h2><div class="grid">${Object.keys(SLOTS).map(s => S.eq[s]
      ? itemHtml(S.eq[s], itemButtons(S.eq[s], true)) : `<div class="card muted">${SLOTS[s]}: leer</div>`).join('')}</div></div>
    <div class="panel"><div class="row between"><h2>Rucksack (${inv.length})</h2>
      ${inv.some(i => i.rar === 0) ? '<button class="small" data-act="sellcommon">Alle gewöhnlichen verkaufen</button>' : ''}</div>
      ${inv.length ? `<div class="grid">${inv.map(i => itemHtml(i, itemButtons(i, false))).join('')}</div>` : '<p class="muted">Leer. Schmiede dir etwas!</p>'}</div>`;
}
function charHtml() {
  const st = stats();
  const attr = (k, label, hint) => `<div class="stat"><span>${label} <b>${S[k]}</b> <span class="muted">${hint}</span></span>
    <button class="small" data-act="attr" data-a="${k}" ${S.pts ? '' : 'disabled'}>+1</button></div>`;
  return `<div class="grid"><div class="panel"><h2>Attribute <span class="muted">(${S.pts} Punkte frei)</span></h2>
      ${attr('str', 'Stärke', '+2 Angriff')}${attr('vit', 'Ausdauer', '+12 Leben')}${attr('agi', 'Geschick', '+1 % Krit, +1,5 % Tempo')}</div>
    <div class="panel"><h2>Werte</h2>
      <div class="stat"><span>Stufe</span><b>${S.lvl} (${S.xp}/${xpNeed(S.lvl)} EP)</b></div>
      <div class="stat"><span>Angriff</span><b>${st.atk}</b></div><div class="stat"><span>Rüstung</span><b>${st.def}</b></div>
      <div class="stat"><span>Leben</span><b>${st.hp}</b></div><div class="stat"><span>Kritische Treffer</span><b>${st.crit} %</b></div>
      <div class="stat"><span>Schläge pro Sekunde</span><b>${(1 / st.interval).toFixed(2)}</b></div>
      <div class="stat"><span>Lebensraub</span><b>${st.ls} %</b></div><div class="stat"><span>Doppelschlag</span><b>${st.dbl} %</b></div></div></div>
    <div class="panel"><button class="small" data-act="reset">${confirmReset ? 'Wirklich alles löschen? Zum Bestätigen noch einmal tippen' : 'Spielstand löschen und neu beginnen'}</button></div>`;
}
function potionPrice() { return 20 * S.zones; }
function shopHtml() {
  const p = potionPrice();
  return `<div class="panel"><h2>Händlerin Mirla</h2><p class="muted">„Ein Schmied, der kämpft? Dann wirst du die hier brauchen.“</p>
    <div class="card"><b>🧪 Heiltrank</b><div class="muted">Heilt im Kampf 40 % deines Lebens. Du besitzt ${S.potions}.</div>
    <div class="btns"><button data-act="buy" data-a="1" ${S.gold >= p ? '' : 'disabled'}>1 kaufen (${p}🪙)</button>
    <button data-act="buy" data-a="5" ${S.gold >= p * 5 ? '' : 'disabled'}>5 kaufen (${p * 5}🪙)</button></div></div></div>`;
}

// ---------------------------------------------------------------- Aktionen
const ACT = {
  tab(a) { if (C && a !== 'adventure') leaveFight(); tab = a; confirmReset = false; },
  fight(a) { startFight(+a, false); },
  boss(a) { if (S.kills[+a] >= BOSS_KILLS) startFight(+a, true); },
  leave() { leaveFight(); },
  auto() { autoNext = !autoNext; },
  smash() { if (C && !C.over && C.cdSmash <= 0) { C.cdSmash = 6; playerHit(2.5, '💥 Wuchtschlag trifft'); if (C.ehp <= 0) endFight(true); } return false; },
  guard() { if (C && !C.over && C.cdGuard <= 0) { C.cdGuard = 12; C.guard = 4; logC('🛡️ Eisenhaut: 4 Sekunden lang halber Schaden', 'good'); } return false; },
  potion() {
    if (C && !C.over && C.cdPotion <= 0 && S.potions > 0) {
      const st = stats(), heal = Math.round(st.hp * 0.4);
      S.potions--; C.cdPotion = 5; C.hp = Math.min(st.hp, C.hp + heal);
      logC(`🧪 Heiltrank: +${heal} Leben`, 'good'); renderHud();
    }
    return false;
  },
  forge(a, b) { startForge(+a, +b); return false; },
  strike() { strike(); return false; },
  equip(a) {
    const i = S.inv.findIndex(x => x.id === +a); if (i < 0) return;
    const it = S.inv.splice(i, 1)[0], old = S.eq[it.slot];
    S.eq[it.slot] = it; if (old) S.inv.push(old);
  },
  sell(a) {
    const i = S.inv.findIndex(x => x.id === +a); if (i < 0) return;
    S.gold += S.inv[i].value; S.inv.splice(i, 1);
  },
  sellcommon() {
    let g = 0; S.inv = S.inv.filter(i => i.rar > 0 || (g += i.value, false));
    S.gold += g; toast(`Verkauft für ${g} Gold`);
  },
  enhance(a) {
    const it = findItem(+a); if (!it || it.plus >= 10) return;
    const c = enhanceCost(it); if (S.gold < c.gold || S.mats[it.tier] < c.ore) return;
    S.gold -= c.gold; S.mats[it.tier] -= c.ore;
    if (Math.random() * 100 < enhanceChance(it)) { it.plus++; toast(`🔨 Erfolg! <b>${it.name} +${it.plus}</b>`); gainSmithXp(5 * (it.tier + 1)); }
    else toast('💨 Die Verstärkung ist fehlgeschlagen.');
  },
  attr(a) { if (S.pts > 0) { S[a]++; S.pts--; } },
  buy(a) { const n = +a, p = potionPrice() * n; if (S.gold >= p) { S.gold -= p; S.potions += n; } },
  reset() { if (!confirmReset) { confirmReset = true; return; } confirmReset = false; leaveFight(); F = null; lastForged = null; newGame(); tab = 'adventure'; },
};
document.addEventListener('click', e => {
  const el = e.target.closest('[data-act]'); if (!el || el.disabled) return;
  const r = ACT[el.dataset.act](el.dataset.a, el.dataset.b);
  if (r === false) { if (C) updateCombat(); save(); } else render();
});
document.addEventListener('keydown', e => {
  if (e.code === 'Space' && F && tab === 'forge') { e.preventDefault(); strike(); }
});

load();
render();
