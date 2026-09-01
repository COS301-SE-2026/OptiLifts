import http from 'k6/http';
import { sleep, check } from 'k6';
import { Trend } from 'k6/metrics';

const baselineTrend = new Trend('baseline_response_time');
const scaledTrend = new Trend('scaled_response_time');

export const options = {
  scenarios: {
    baseline_phase: {
      executor: 'ramping-vus',
      startVUs: 0,
      stages: [
        { duration: '30s', target: 100 },
        { duration: '1m', target: 100 },
      ],
      exec: 'runBaseline',
    },
    scaled_phase: {
      executor: 'ramping-vus',
      startVUs: 100,
      stages: [
        { duration: '30s', target: 300 },
        { duration: '1m', target: 300 },
      ],
      startTime: '1m30s',
      exec: 'runScaled',
    }
  },
};


const TEST_USER = {
  email: 'gymgoer@gmail.com',
  password: 'GymGoer123!'   //NOSONAR
};
const BASE_URL = 'https://api.optilifts.app/api';

export function setup() {
  const loginRes = http.post(`${BASE_URL}/auth/login`, JSON.stringify(TEST_USER), {
    headers: { 'Content-Type': 'application/json' },
  });

  if (loginRes.status !== 200) {
    console.error(`LOGIN FAILED! Status: ${loginRes.status}`);
    throw new Error('Authentication failed');
  }

  let authCookie = '';
  if (loginRes.cookies?.['access_token']) {
    authCookie = loginRes.cookies['access_token'][0].value;
  } else {
    throw new Error('Missing access_token cookie');
  }
  return { cookie: authCookie };
}

// baseline with 100 users
export function runBaseline(data) {
  const params = { headers: { 'Cookie': `access_token=${data.cookie}` } };
  const res = http.get(`${BASE_URL}/workouts`, params);

  check(res, {
    'baseline success (200)': (r) => r.status === 200
  });
  baselineTrend.add(res.timings.duration);
  sleep(1);
}

// scale up to 300 users
export function runScaled(data) {
  const params = {
    headers: {
      'Cookie': `access_token=${data.cookie}`
    }
  };
  const res = http.get(`${BASE_URL}/workouts`, params);

  check(res, {
    'scaled success (200)': (r) => r.status === 200
  });
  scaledTrend.add(res.timings.duration);
  sleep(1);
}
