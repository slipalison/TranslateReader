'use strict';

// Node >= 24 treats every positional argument of `node --test` as a glob pattern,
// so `node --test test/js/` matches this directory and runs it as a single entry
// point instead of expanding it. This aggregator makes that invocation load every
// `*.test.js` sibling; discovery is dynamic, so a new test file can never be
// silently left out of the run.

const fs = require('node:fs');
const path = require('node:path');

const files = fs.readdirSync(__dirname)
    .filter((name) => name.endsWith('.test.js'))
    .sort();

for (const name of files) {
    require(path.join(__dirname, name));
}
