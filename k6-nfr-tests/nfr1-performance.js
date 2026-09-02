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
        { duration: '30s', target: 100 },
        { duration: '1m', target: 100 },  //hold 100 after ramping up
      ],
      startTime: '35s',
      exec: 'runLoad',
    }
  },
  thresholds: {
    'load_response_time': ['p(95)<1500'],
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

// nfr1.1 test
export function runBaseline(data) {
  const params = { headers: { 'Cookie': `access_token=${data.cookie}` } };
  const res = http.get(`${BASE_URL}/workouts`, params);

  check(res, { 'baseline success': (r) => r.status === 200 });
  baselineTrend.add(res.timings.duration); // Record the baseline time
  sleep(1);
}

// nfr1.3 test
export function runLoad(data) {
  const params = {
    headers: {
      'Cookie': `access_token=${data.cookie}`
    }
  };

  const res = http.get(`${BASE_URL}/workouts`, params);

  check(res, {
    'load success': (r) => r.status === 200
  });

  loadTrend.add(res.timings.duration);
  sleep(1);
}
