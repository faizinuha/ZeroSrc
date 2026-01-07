#!/usr/bin/env node

const https = require('https');
const { exec } = require('child_process');
const os = require('os');
const fs = require('fs');
const path = require('path');

const REPO = 'faizinuha/ZeroMix';
const API_URL = `https://api.github.com/repos/${REPO}/releases`;
const GITHUB_RELEASE_URL = `https://github.com/${REPO}/releases`;

// Color output untuk terminal
const colors = {
  reset: '\x1b[0m',
  bright: '\x1b[1m',
  dim: '\x1b[2m',
  green: '\x1b[32m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  cyan: '\x1b[36m',
  red: '\x1b[31m'
};

function log(message, color = 'reset') {
  console.log(`${colors[color]}${message}${colors.reset}`);
}

function getCurrentVersion() {
  // Cek di registry Windows atau file version
  if (os.platform() === 'win32') {
    return new Promise((resolve) => {
      exec('reg query HKCU\\Software\\ZeroMix /v Version 2>nul', (err, stdout) => {
        if (err || !stdout) {
          resolve('0.0.0');
          return;
        }
        const match = stdout.match(/Version\s+REG_SZ\s+([^\r\n]+)/);
        resolve(match ? match[1] : '0.0.0');
      });
    });
  }
  return Promise.resolve('0.0.0');
}

function normalizeVersion(version) {
  // Konversi "v2.1.0" ke 210 untuk perbandingan
  const cleaned = version.replace(/^v|[^0-9.]/g, '');
  return parseInt(cleaned.split('.').join('').padEnd(3, '0'));
}

function openBrowser(url) {
  const command = os.platform() === 'win32' 
    ? `start "" "${url}"`
    : os.platform() === 'darwin'
    ? `open "${url}"`
    : `xdg-open "${url}"`;
  
  exec(command, (err) => {
    if (err) {
      log(`\n⚠️  Buka link manual: ${url}`, 'yellow');
    }
  });
}

function fetchReleases() {
  return new Promise((resolve, reject) => {
    const options = {
      hostname: 'api.github.com',
      path: `/repos/${REPO}/releases`,
      method: 'GET',
      headers: { 'User-Agent': 'ZeroMix-CLI' }
    };

    https.request(options, (res) => {
      let data = '';
      res.on('data', chunk => data += chunk);
      res.on('end', () => {
        try {
          resolve(JSON.parse(data));
        } catch (e) {
          reject(new Error('Gagal parse GitHub API response'));
        }
      });
    }).on('error', reject).end();
  });
}

async function checkForUpdates() {
  try {
    log('\n🔍 Memeriksa update...\n', 'cyan');

    // Ambil rilis dari GitHub
    const releases = await fetchReleases();
    
    if (!releases || releases.length === 0) {
      log('❌ Tidak ada release ditemukan', 'red');
      return;
    }

    // Cek versi saat ini
    const currentVersion = await getCurrentVersion();
    const currentNorm = normalizeVersion(currentVersion);
    
    log(`Versi saat ini: ${currentVersion}`, 'dim');

    // Cari latest stable release
    let stableRelease = null;
    let latestNorm = 0;

    for (const release of releases) {
      // Skip pre-release untuk stable channel
      if (release.prerelease) continue;
      
      const releaseNorm = normalizeVersion(release.tag_name);
      if (releaseNorm > latestNorm) {
        latestNorm = releaseNorm;
        stableRelease = release;
      }
    }

    if (!stableRelease) {
      log('Tidak ada release stable tersedia', 'yellow');
      return;
    }

    // Cek apakah ada update
    if (latestNorm <= currentNorm) {
      log('\n✅ Aplikasi sudah terbaru Kak...!', 'green');
      log(`\n📍 Release terbaru: ${stableRelease.tag_name}`, 'dim');
      if (stableRelease.body) {
        log(`\n📝 Changelog:\n${stableRelease.body.substring(0, 300)}...`, 'dim');
      }
      log(`\n🔗 Lihat semua release: ${GITHUB_RELEASE_URL}`, 'blue');
      return;
    }

    // Ada update tersedia
    log('\n🎉 Update tersedia!\n', 'green');
    log(`Versi terbaru: ${stableRelease.tag_name}`, 'bright');
    
    if (stableRelease.body) {
      log(`\n📝 Changelog:\n${stableRelease.body.substring(0, 400)}`, 'dim');
    }

    // Download file
    const installerFile = stableRelease.assets?.find(a => a.name.includes('Setup') || a.name.includes('setup'));
    if (installerFile) {
      log(`\n💾 Ukuran: ${(installerFile.size / 1024 / 1024).toFixed(2)} MB`, 'dim');
    }

    // Tanyakan user
    log('\n➡️  Buka halaman download? (y/n): ', 'yellow');
    
    process.stdin.once('data', (input) => {
      const answer = input.toString().trim().toLowerCase();
      if (answer === 'y' || answer === 'yes') {
        log('\n🌐 Membuka browser...', 'cyan');
        openBrowser(stableRelease.html_url);
      } else {
        log('\n✋ Dibatalkan.', 'dim');
      }
    });

  } catch (error) {
    log(`\n❌ Error: ${error.message}`, 'red');
    log(`\n💡 Alternatif:\n   📍 GitHub: ${GITHUB_RELEASE_URL}`, 'yellow');
    log(`   🌐 Website: https://zeromix.vercel.app`, 'yellow');
  }
}

// Main entry point
const command = process.argv[2];

if (command === 'cek-update' || command === 'cek' || !command) {
  checkForUpdates();
} else if (command === 'version') {
  log('ZeroMix CLI v1.0.0', 'blue');
} else if (command === '--help' || command === '-h') {
  log('\n📋 ZeroMix Update Checker', 'bright');
  log('\nPenggunaan:', 'cyan');
  log('  zeromix-cli [command]', 'dim');
  log('\nCommands:', 'cyan');
  log('  cek-update    Cek update aplikasi (default)', 'dim');
  log('  version       Tampilkan versi CLI', 'dim');
  log('  --help        Tampilkan bantuan ini', 'dim');
  log('\nContoh:', 'cyan');
  log('  zeromix-cli cek-update', 'dim');
  log('  zeromix-cli', 'dim');
  log('');
} else {
  log(`❌ Perintah tidak dikenal: ${command}`, 'red');
  log('Gunakan: zeromix-cli --help', 'yellow');
}
