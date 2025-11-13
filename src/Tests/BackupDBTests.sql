TRUNCATE TABLE public."Players" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."WorkshopItems" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."LevelWorkshopItems" RESTART IDENTITY CASCADE;
TRUNCATE TABLE public."MachineWorkshopItems" RESTART IDENTITY CASCADE;

INSERT INTO public."Players" ("Id", "Username", "Role", "Password")
VALUES
(1, 'Alice', 'User', '19uwdt0wa0Q2wYA6U9I9/g=='),
(2, 'Bob', 'Admin', '19uwdt0wa0Q2wYA6U9I9/g=='),
(3, 'Charlie', 'User', '19uwdt0wa0Q2wYA6U9I9/g=='),
(4, 'Diana', 'Moderator', '19uwdt0wa0Q2wYA6U9I9/g=='),
(5, 'Eve', 'User', '19uwdt0wa0Q2wYA6U9I9/g=='),
(6, 'Frank', 'User', '19uwdt0wa0Q2wYA6U9I9/g=='),
(7, 'Grace', 'Admin', '19uwdt0wa0Q2wYA6U9I9/g=='),
(8, 'Heidi', 'User', '19uwdt0wa0Q2wYA6U9I9/g=='),
(9, 'Ivan', 'User', '19uwdt0wa0Q2wYA6U9I9/g=='),
(10, 'Judy', 'User', '19uwdt0wa0Q2wYA6U9I9/g==');

SELECT setval(pg_get_serial_sequence('"Players"', 'Id'), COALESCE(MAX("Id"), 1)) FROM "Players";