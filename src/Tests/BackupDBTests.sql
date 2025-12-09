TRUNCATE TABLE public."Players" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."WorkshopItems" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."LevelWorkshopItems" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."MachineWorkshopItems" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."Lobbies" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."AdminLogs" RESTART IDENTITY CASCADE;


INSERT INTO public."Players" ("Id", "Username", "Role", "Password")
VALUES
(1, 'Alice', 'User', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(2, 'Bob', 'Admin', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(3, 'Charlie', 'User', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(4, 'Diana', 'Moderator', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(5, 'Eve', 'User', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(6, 'Frank', 'User', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(7, 'Grace', 'Admin', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(8, 'Heidi', 'User', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(9, 'Ivan', 'User', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU='),
(10, 'Judy', 'User', 'OQMNCzUYK4l4iF0wS/pV8w==.Tn1KYu6PBmd51kaHle9GJefeqw0KVQ25Bw+7bWSz1hU=');

SELECT setval(pg_get_serial_sequence('"Players"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "Players";
SELECT setval(pg_get_serial_sequence('"AdminLogs"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "AdminLogs";