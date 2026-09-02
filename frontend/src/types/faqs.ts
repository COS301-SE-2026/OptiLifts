import type { FaqItem } from "@/components/help/faq-accordion";

const RAW_DATA: readonly [string, string, string, string][] = [
    //NOSONAR
    [
        'faq-1', 'Workouts',  'How do I create and customise a workout routine?',  'Navigate to the "Workouts" page via the top navbar, then click the + button. You can name your workout, add exercises, set target sets and repititions, and create custom exercises.',//NOSONAR
    ],
    [
        'faq-2','Offline Sync', 'What happens if I lose internet connection during an active workout?',  'OptiLifts has full offline logging capabilities. Your sets, weights, and reps are automatically stored locally in your bwoser. When your connection is restored, your information sync back to the server.',//NOSONAR
    ],
    [
        'faq-3','Schedule', 'How do I schedule workout sessions on specific days?',  'Go to the "Schedule" page to view your weekly calendar. Click on the day you wish to assign a workout to, and select the routine to schedule.',//NOSONAR
    ],
    [
        'faq-4','Profile', 'Where can I view my workout history and total lifting stats?',  'Go to the "Profile" page to see your logged sections, and click on "View All" to see total volume lifted, and summaries of each of your completed sessions. ',//NOSONAR
    ],
    [
        'faq-5','Schedule', 'How do I connect my schedule to Google Calendar?',  'Go to the "Schedule" page and click on the "Settings" button. Select the "Sync with Google Calendar" button, and pick which Google Account you wish to connect with.',//NOSONAR
    ],
    [
        'faq-6','Schedule', 'How does OptiLifts sync with my Google Calendar?',  'When you connect Google Calendar in your account settings, OptiLifts creates a dedicated calendar named "OptiLifts" in your Google account. Upcoming scheduled workout sessions are automatically synced to this calendar, as well as exercise lists and sets.',//NOSONAR
    ],
    [
        'faq-7','Schedule', 'What happens if I disconnect or reconnect my Google Calendar?',  'Disconnecting revokes calendar permissions and turns off sync settings. When you reconnect, OptiLifts re-authenticates with Google, reconnects to your calendar, and automatically re-sync all upcoming workouts.',//NOSONAR
    ],
    [
        'faq-8', 'Schedule',  'If I reschedule or edit a workout in OptiLifts, will Google Calendar update?',  'Yes. Whenever you add, modify, or reschedule a workout session in OptiLifts, all future events in your connected Google Calendar are updated to match your new schedule.',//NOSONAR
    ],
    [
        'faq-9', 'Schedule',  'How does OptiLifts handle missed workout sessions?',  'Uncompleted workouts past their scheduled time are flagged as "Missed". Triggering a reschedule lets OptiLifts evaluate all selected missed entries and suggest new dates that fit your schedule.',//NOSONAR
    ],
    [
        'faq-10', 'Schedule', 'What is the Dynamic Scheduler and how does it work?',   'The Dynamic Scheduler is an automated planning engine that adjusts your trainign routine when life gets in the way. If you miss sessions or need to change your routine, it calculated a revised workout schedule while protecting your recovery and rest constraints, such as muscle rest periods and mandatory rest days.',//NOSONAR
    ],
    [
        'faq-11','Schedule',  'Why was one of my missed workouts marked as "Dropped" during rescheduling?',  'A session is marked as "Dropped" if fitting it into your remaining cycle window would violate your max daily workout limit, rest day settings, or muscle recovery rules. Dropping over-constrained workouts prevents schedule compounding and burnout.',//NOSONAR
    ],
    [
        'faq-12','Schedule',  'Does the Dynamic Scheduler check for conflicts with external personal meetings or events?',  'The Dynamic Scheduler optimises based on your OptiLifts schedule configuration (rest days, max workouts per day, rest hours). We recommend setting your preferred weekly rest days and daily caps in OptiLifts to reflect your overall real-world availability.',//NOSONAR
    ],
];
export const FAQ_DATA: readonly FaqItem[] = RAW_DATA.map(
    ([id, category, question, answer]) => ({ id, category, question, answer })
)