// Single reusable SSH command runner. Usage: node ssh.js "command"
const { Client } = require('ssh2');
const cmd = process.argv.slice(2).join(' ');
if (!cmd) { console.error('Usage: node ssh.js "command"'); process.exit(1); }
const c = new Client();
c.on('ready', () => {
  c.exec(cmd, (err, s) => {
    if (err) { console.error(err); c.end(); return; }
    let out = '';
    s.on('data', d => out += d);
    s.stderr.on('data', d => out += d);
    s.on('close', () => { console.log(out); c.end(); });
  });
}).connect({ host: '209.61.36.86', port: 22, username: 'root', password: ';A8zcwBwUfMMJ.(,[ZLk' });
