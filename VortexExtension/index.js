const fs = require('fs');
const path = require('path');

function main(context) {
    context.api.events.on('did-deploy', () => {
        try {
            const state = context.api.store.getState();
            const appData = process.env.APPDATA;
            const dumpPath = path.join(appData, 'Vortex', 'live_state.json');
            fs.writeFileSync(dumpPath, JSON.stringify(state));
        } catch (err) {}
    });
    return true;
}

module.exports = { default: main };