// ============================================================
// เซิร์ฟเวอร์กู้คืนข้อมูล - ใช้รหัส 6 ตัวอักษร
// รันด้วย: node server.js
// ============================================================
const http = require('http');
const fs = require('fs');
const path = require('path');
const url = require('url');

const DATA_FILE = path.join(__dirname, 'restore_data.json');
const PORT = 3000;

// อ่านข้อมูลจากไฟล์
function loadData() {
    try {
        if (fs.existsSync(DATA_FILE)) {
            return JSON.parse(fs.readFileSync(DATA_FILE, 'utf8'));
        }
    } catch(e) {}
    return {};
}

// เขียนข้อมูลลงไฟล์
function saveData(data) {
    fs.writeFileSync(DATA_FILE, JSON.stringify(data), 'utf8');
}

// สร้างรหัส 6 ตัวอักษร (ตัวเลข + ตัวอักษรพิมพ์ใหญ่ ไม่รวม O,0,I,1)
function generateCode() {
    const chars = 'ABCDEFGHJKLMNPQRSTUVWXYZ23456789';
    let code = '';
    for (let i = 0; i < 6; i++) {
        code += chars[Math.floor(Math.random() * chars.length)];
    }
    return code;
}

// แยกประเภทย่อย request
function parseBody(req, cb) {
    let body = '';
    req.on('data', chunk => body += chunk);
    req.on('end', () => {
        try {
            cb(JSON.parse(body));
        } catch(e) {
            cb(null);
        }
    });
}

const server = http.createServer((req, res) => {
    // CORS headers
    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');
    res.setHeader('Content-Type', 'application/json');

    if (req.method === 'OPTIONS') {
        res.writeHead(200);
        res.end();
        return;
    }

    const parsedUrl = url.parse(req.url, true);
    const pathname = parsedUrl.pathname;

    // POST /save - บันทึกข้อมูลและรับรหัสกู้คืน
    if (req.method === 'POST' && pathname === '/save') {
        parseBody(req, (data) => {
            if (!data || !data.email || !data.data) {
                res.writeHead(400);
                res.end(JSON.stringify({ error: 'ข้อมูลไม่ถูกต้อง' }));
                return;
            }
            const db = loadData();
            const code = generateCode();
            // เก็บรหัสไว้ 30 วัน
            db[code] = {
                email: data.email,
                data: data.data,
                time: Date.now()
            };
            // ลบรหัสที่หมดอายุ (30 วัน)
            const expireTime = Date.now() - 30 * 24 * 60 * 60 * 1000;
            Object.keys(db).forEach(key => {
                if (db[key].time < expireTime) delete db[key];
            });
            saveData(db);
            res.writeHead(200);
            res.end(JSON.stringify({ success: true, code: code }));
        });
        return;
    }

    // POST /load - นำเข้าข้อมูลด้วยรหัสกู้คืน
    if (req.method === 'POST' && pathname === '/load') {
        parseBody(req, (data) => {
            if (!data || !data.code) {
                res.writeHead(400);
                res.end(JSON.stringify({ error: 'กรุณากรอกรหัสกู้คืน' }));
                return;
            }
            const code = data.code.toUpperCase().trim();
            const db = loadData();
            if (!db[code]) {
                res.writeHead(404);
                res.end(JSON.stringify({ error: '❌ รหัสกู้คืนไม่ถูกต้องหรือหมดอายุแล้ว' }));
                return;
            }
            const result = { email: db[code].email, data: db[code].data };
            // ลบรหัสหลังจากใช้งาน (ใช้ครั้งเดียว)
            delete db[code];
            saveData(db);
            res.writeHead(200);
            res.end(JSON.stringify({ success: true, email: result.email, data: result.data }));
        });
        return;
    }

    // GET / - หน้าเว็บ
    if (req.method === 'GET' && pathname === '/') {
        // ส่ง HTML หน้าแรก
        res.setHeader('Content-Type', 'text/html; charset=utf-8');
        const html = `<!DOCTYPE html>
<html lang="th">
<head>
    <meta charset="UTF-8">
    <meta name="viewport" content="width=device-width, initial-scale=1.0">
    <title>🐱 ระบบกู้คืนข้อมูลเกม</title>
    <style>
        * { margin:0; padding:0; box-sizing:border-box; }
        body {
            font-family: 'Segoe UI', sans-serif;
            background: linear-gradient(135deg, #0f0c29, #302b63, #24243e);
            min-height: 100vh; display: flex; justify-content: center; align-items: center;
            color: #fff; padding: 20px;
        }
        .container {
            background: rgba(255,255,255,0.05);
            backdrop-filter: blur(10px);
            border-radius: 20px; padding: 30px;
            max-width: 500px; width: 100%;
            box-shadow: 0 0 60px rgba(138,43,226,0.3);
            border: 1px solid rgba(255,255,255,0.1);
        }
        h1 { text-align: center; font-size: 24px; margin-bottom: 5px; }
        p { text-align: center; color: #aaa; margin-bottom: 25px; font-size: 14px; }
        .section { margin-bottom: 20px; padding: 15px; border-radius: 12px; background: rgba(255,255,255,0.05); }
        .section h3 { font-size: 16px; margin-bottom: 10px; }
        input, textarea { width: 100%; padding: 10px; border-radius: 8px; border: 1px solid rgba(255,255,255,0.2); background: rgba(255,255,255,0.08); color: #fff; font-size: 13px; outline: none; margin-bottom: 8px; }
        input:focus, textarea:focus { border-color: #f5576c; }
        button {
            width: 100%; padding: 12px; border-radius: 10px; border: none;
            font-size: 15px; font-weight: bold; cursor: pointer; transition: 0.3s;
        }
        .btn-save { background: linear-gradient(135deg, #f093fb, #f5576c); color: #fff; }
        .btn-load { background: linear-gradient(135deg, #43e97b, #38f9d7); color: #222; }
        .btn-load:hover, .btn-save:hover { transform: scale(1.02); }
        .msg { margin-top: 5px; font-size: 13px; min-height: 20px; }
        .success { color: #43e97b; }
        .error { color: #f5576c; }
        .code-display { font-size: 36px; font-weight: bold; text-align: center; letter-spacing: 8px; padding: 15px; background: rgba(15,12,41,0.9); border: 2px solid #43e97b; border-radius: 12px; margin: 10px 0; }
        small { color: #888; font-size: 11px; }
        .footer { text-align: center; margin-top: 20px; font-size: 12px; color: #666; }
    </style>
</head>
<body>
<div class="container">
    <h1>🐱 ระบบกู้คืนข้อมูลเกม</h1>
    <p>เซิร์ฟเวอร์กำลังทำงาน... (รหัส 6 ตัว สั้นๆ)</p>
    
    <div class="section">
        <h3>📤 ส่งออกข้อมูล (สร้างรหัสกู้คืน)</h3>
        <input type="text" id="save-email" placeholder="อีเมลที่ใช้ในเกม...">
        <textarea id="save-data" rows="4" placeholder="วางข้อความกู้คืนจากเกมที่นี่..."></textarea>
        <button class="btn-save" onclick="saveToServer()">🔑 สร้างรหัสกู้คืน 6 หลัก</button>
        <div class="msg" id="save-msg"></div>
        <div id="code-result" style="display:none;">
            <p style="color:#aaa;font-size:13px;margin:5px 0;">✅ รหัสกู้คืนของคุณ (6 ตัวอักษร):</p>
            <div class="code-display" id="result-code"></div>
            <p style="color:#888;font-size:11px;text-align:center;">รหัสนี้ใช้ได้ 30 วัน ใช้ครั้งเดียว</p>
        </div>
    </div>

    <div class="section">
        <h3>📥 นำเข้าข้อมูล (ใช้รหัสกู้คืน)</h3>
        <input type="text" id="load-code" placeholder="รหัสกู้คืน 6 ตัวอักษร..." maxlength="6" style="text-transform:uppercase;letter-spacing:4px;font-size:20px;text-align:center;">
        <button class="btn-load" onclick="loadFromServer()">📦 กู้คืนข้อมูล</button>
        <div class="msg" id="load-msg"></div>
    </div>

    <div class="footer">
        ⚡ เซิร์ฟเวอร์รันที่ port ${PORT} | ข้อมูลถูกเก็บในไฟล์ restore_data.json
    </div>
</div>

<script>
async function saveToServer() {
    const email = document.getElementById('save-email').value.trim();
    const dataStr = document.getElementById('save-data').value.trim();
    const msgEl = document.getElementById('save-msg');
    if (!email) { msgEl.innerHTML = '❌ กรุณากรอกอีเมล'; msgEl.className = 'msg error'; return; }
    if (!dataStr) { msgEl.innerHTML = '❌ กรุณาวางข้อความกู้คืนจากเกม'; msgEl.className = 'msg error'; return; }
    try {
        const data = JSON.parse(decodeURIComponent(escape(atob(dataStr))));
        const res = await fetch('/save', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, data })
        });
        const result = await res.json();
        if (result.success) {
            msgEl.innerHTML = '✅ สร้างรหัสกู้คืนสำเร็จ!';
            msgEl.className = 'msg success';
            document.getElementById('result-code').textContent = result.code;
            document.getElementById('code-result').style.display = 'block';
        } else {
            msgEl.innerHTML = '❌ ' + (result.error || 'เกิดข้อผิดพลาด');
            msgEl.className = 'msg error';
        }
    } catch(e) {
        msgEl.innerHTML = '❌ ข้อความกู้คืนไม่ถูกต้อง';
        msgEl.className = 'msg error';
    }
}

async function loadFromServer() {
    const code = document.getElementById('load-code').value.trim().toUpperCase();
    const msgEl = document.getElementById('load-msg');
    if (!code || code.length !== 6) { msgEl.innerHTML = '❌ กรุณากรอกรหัส 6 ตัวอักษร'; msgEl.className = 'msg error'; return; }
    const res = await fetch('/load', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({ code })
    });
    const result = await res.json();
    if (result.success) {
        msgEl.innerHTML = '✅ กู้คืนสำเร็จ! <br>📋 คัดลอกข้อความด้านล่างไปวางในเกม:<br><textarea rows="3" style="width:100%;margin-top:5px;padding:8px;border-radius:6px;background:rgba(255,255,255,0.08);color:#fff;font-size:11px;border:1px solid rgba(255,255,255,0.2);" readonly>' + btoa(unescape(encodeURIComponent(JSON.stringify({email:result.email,data:result.data})))) + '</textarea>';
        msgEl.className = 'msg success';
    } else {
        msgEl.innerHTML = '❌ ' + (result.error || 'เกิดข้อผิดพลาด');
        msgEl.className = 'msg error';
    }
}
</script>
</body>
</html>`;
        res.end(html);
        return;
    }

    // 404
    res.writeHead(404);
    res.end(JSON.stringify({ error: 'ไม่พบหน้า' }));
});

server.listen(PORT, () => {
    console.log('==================================');
    console.log('🐱 ระบบกู้คืนข้อมูลเกม');
    console.log('==================================');
    console.log(`🔗 เปิดในบราวเซอร์: http://localhost:${PORT}`);
    console.log(`📁 ข้อมูลเก็บใน: restore_data.json`);
    console.log(`🔄 รหัสกู้คืน: 6 ตัวอักษร`);
    console.log(`⏰ อายุรหัส: 30 วัน`);
    console.log('==================================');
    console.log('⚠️ ถ้าต้องการให้คนอื่นใช้งานได้');
    console.log('   ต้อง deploy ขึ้น server (Render/Vercel/etc)');
    console.log('==================================');
});