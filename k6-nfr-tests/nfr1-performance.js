import http from 'k6/http';
import { sleep, check } from 'k6';
import { Trend } from 'k6/metrics';

const baselineTrend = new Trend('baseline_response_time');
const loadTrend = new Trend('load_response_time');

export const options = {
  scenarios: {
    //single user
    baseline: {
      executor: 'constant-vus',
      vus: 1,
      duration: '30s',
      exec: 'runBaseline',
    },
    // 100 user load test
    load: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 100 }, // ramp up to 15 (live system) or 100 users (prod overlay)
        { duration: '1m', target: 100 },  // hold 15 (live system) or 100 users (prod overlay)
      ],
      startTime: '35s',
      exec: 'runLoad',
    }
  },
  thresholds: {
    'load_response_time': ['p(95)<500'],
  },
};

const TEST_USER = {
  email: 'gymgoer@gmail.com',
  password: 'GymGoer123!'   //NOSONAR
};
const BASE_URL = 'http://localhost:8080/api';

export function setup() {
  const loginRes = http.post(`${BASE_URL}/auth/login`, JSON.stringify(TEST_USER), {
    headers: { 'Content-Type': 'application/json' },
  });

  if (loginRes.status !== 200) {
    throw new Error('Authentication failed');
  }

  let authCookie = '';
  if (loginRes.cookies?.['access_token']) {
    authCookie = loginRes.cookies['access_token'][0].value;
  } else {
    throw new Error('Missing access token cookie');
  }
  return { cookie: authCookie };
}

export function runBaseline(data) {
  executeUserWorkflow(data.cookie);
}

export function runLoad(data) {
  executeUserWorkflow(data.cookie);
}

function executeUserWorkflow(cookie) {
  const params = { headers: { 'Cookie': `access_token=${cookie}` } };
  
  const res = http.get(`${BASE_URL}/workouts`, params);

  baselineTrend.add(res.timings.duration);
  loadTrend.add(res.timings.duration);

  check(res, { 'workouts status is 200': (r) => r.status === 200 });

  sleep(1);
}
