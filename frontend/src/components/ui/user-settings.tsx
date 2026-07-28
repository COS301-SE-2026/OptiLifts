import React, { useEffect, useRef, useState } from "react";
import { Eye, EyeOff, ImagePlus, Loader2, X } from "lucide-react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { useAuth } from "@/context/auth-context";
import { toast } from "@/components/ui/alert";
import { CircularProfileImage } from "@/components/ui/circular-image";
import { customFetch } from "@/lib/custom-fetch";
import {
    DropdownMenu,
    DropdownMenuContent,
    DropdownMenuItem,
    DropdownMenuTrigger,
} from "@/components/ui/dropdown-menu";
import { ConfirmDialog } from "@/components/ui/confirm-dialog";
import { metricCheck, outputWeight, inputWeight } from "@/lib/weight-utils";

type UserSettingsPopupProps = Readonly<{
    isOpen: boolean;
    onClose: () => void;
}>;

type ProfileParams = Readonly<{
    profile: {
        displayName: string;
        bio: string;
        sex: string;
        dateOfBirth: string;
        weight: string;
        height: string;
    };
    updateProfile: (field: string, value: string) => void;
    selectedImgUrl: string | null;
    setSelectedImg: (file: File | null) => void;
    setSelectedImgUrl: (url: string | null) => void;
    error?: string | null;
}>;

type PreferencesParams = Readonly<{
    preferences: {
        theme: string;
        units: string;
    };
    updatePreferences: (field: string, value: string) => void;
    error?: string | null;
}>;

type SecurityParams = Readonly<{
    security: {
        currentPassword: string;
        newPassword: string;
        confirmPassword: string;
    };
    updateSecurity: (field: string, value: string) => void;
    error?: string | null;
}>;

interface UserSettingsDto {
    profile: {
        displayName: string;
        bio: string;
        sex: string;
        dateOfBirth: string;
        weight: number;
        height: number;
        profilePictureUrl: string | null;
    };

    preferences: {
        theme: string;
        units: string;
    };

}



function useSettingsLogic(isOpen: boolean, onClose: () => void) {
    const { user } = useAuth();

    const initialPreferencesRef = useRef<{ theme: string; units: string } | null>(null);

    const [initialProfilePicUrl, setInitialProfilePicUrl] = useState<string | null>(null);
    const [profileChanged, setProfileChanged] = useState(false);
    const [preferenceChanged, setPreferenceChanged] = useState(false);
    const [securityChanged, setSecurityChanged] = useState(false);

    const [profileError, setProfileError] = useState<string | null>(null);
    const [preferencesError, setPreferencesError] = useState<string | null>(null);
    const [securityError, setSecurityError] = useState<string | null>(null);
    const [generalError, setGeneralError] = useState<string | null>(null);

    //states 
    const [profile, setProfile] = useState({
        displayName: "",
        bio: "",
        sex: "",
        dateOfBirth: "",
        weight: "",
        height: ""
    });

    const [preferences, setPreferences] = useState({
        theme: "dark",
        units: "metric"
    });

    const [security, setSecurity] = useState({
        currentPassword: "",
        newPassword: "",
        confirmPassword: ""
    });

    const [selectedImg, setSelectedImg] = useState<File | null>(null);
    const [selectedImgUrl, setSelectedImgUrl] = useState<string | null>(null);

    const [isLoading, setIsLoading] = useState<boolean>(false);
    const [isSaving, setIsSaving] = useState<boolean>(false);

    // takes field and uses it as key to update state w/ new value 
    // prev => ...prev takes the old object and makes a new one with the old values but updates the new value
    const updateProfile = (field: string, value: string) => {
        setProfile(prev => ({ ...prev, [field]: value }));
        setProfileChanged(true);
    }
    const updatePreferences = (field: string, value: string) => {
        setPreferences(prev => ({ ...prev, [field]: value }));
        setPreferenceChanged(true);
    }
    const updateSecurity = (field: string, value: string) => {
        setSecurity(prev => ({ ...prev, [field]: value }));
        setSecurityChanged(true);
    }

    const deleteProfilePic = async () => {
        const res = await customFetch("/api/users/me/deleteProfilePicture", {
            method: "DELETE"
        });

        if (!res.ok) {
            throw new Error("Failed to delete profile picture.");
        }
    };

    useEffect(() => {
        if (!isOpen) {
            return;
        }

        document.documentElement.classList.toggle("dark", preferences.theme === "dark");
    }, [preferences.theme, isOpen]);

    useEffect(() => {
        if (!isOpen) {
            return;
        }

        async function getSettings() {
            setIsLoading(true);

            setProfileChanged(false);
            setPreferenceChanged(false);
            setSecurityChanged(false);

            setProfileError(null);
            setPreferencesError(null);
            setSecurityError(null);
            setGeneralError(null);

            try {
                const response = await customFetch("/api/users/me/settings");
                if (!response.ok) {
                    throw new Error("Failed to load settings");
                }

                const data: UserSettingsDto = await response.json();

                let formattedHeight= 0;
                if (data.profile.height) {
                    if (metricCheck()) {
                        formattedHeight = data.profile.height;
                    }else{
                        formattedHeight = Math.round(data.profile.height * 0.393701 * 100) / 100
                    }
                }

                setProfile({
                    displayName: data.profile.displayName,
                    bio: data.profile.bio || "",
                    sex: data.profile.sex || "PreferNotToSay",
                    dateOfBirth: data.profile.dateOfBirth ? data.profile.dateOfBirth.split('T')[0] : "",
                    weight: (data.profile.weight) ? outputWeight(data.profile.weight).toString() : "",
                    height: (data.profile.height) ? formattedHeight.toString() : ""
                });

                setSelectedImgUrl(data.profile.profilePictureUrl);
                setInitialProfilePicUrl(data.profile.profilePictureUrl);

                const loadedPreferences = {
                    theme: data.preferences.theme || "dark",
                    units: data.preferences.units || "metric"
                };

                setPreferences(loadedPreferences);
                initialPreferencesRef.current = loadedPreferences;

            } catch (error) {
                const typedError = (error instanceof Error) ? error : new Error("Could not load settings");
                setGeneralError(typedError.message);
            } finally {
                setIsLoading(false);
            }

            setSecurity({
                currentPassword: "",
                newPassword: "",
                confirmPassword: ""
            });

            setSelectedImg(null);
        }

        getSettings();

    }, [isOpen, user]);

    const saveProfileDetails = async () => {

        let unitHeight = null;
        if (profile.height !== "") {
            const parsedHeight = Number.parseFloat(profile.height);
            unitHeight = (metricCheck())? parsedHeight : Math.round(parsedHeight * 2.54);
        }

        const res = await customFetch("/api/users/me/profileDetails", {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                ...profile,
                weight: (profile.weight == "")? null : inputWeight(Number.parseFloat(profile.weight)),
                height: unitHeight
            })
        });

        if (!res.ok) {
            throw new Error("Could not update profile information.");
        }
    };

    const savePreferences = async () => {
        const res = await customFetch("/api/users/me/preferences", {
            method: "PATCH",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(preferences)
        });

        if (!res.ok) {
            throw new Error("Could not update preferences.");
        }

        if (preferences.theme === "dark") {
            document.documentElement.classList.add("dark");
        } else {
            document.documentElement.classList.remove("dark");
        }

        localStorage.setItem("theme", preferences.theme);
        localStorage.setItem("units", preferences.units);

        window.location.reload();
    };

    const savePassword = async () => {

        if (security.newPassword === "" || security.currentPassword === "" || security.confirmPassword === "") {
            throw new Error("All password fields are required.");
        }

        if (security.newPassword !== security.confirmPassword) {
            throw new Error("New passwords do not match.");
        }

        if (security.currentPassword === "") {
            throw new Error("Enter current password");
        }

        const passwordRegex = /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^A-Za-z0-9]).{8,}$/;
        if (!passwordRegex.test(security.newPassword)) {
            throw new Error("New password does not meet complexity requirements.");
        }

        const res = await customFetch("/api/users/me/updatePassword", {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({
                currentPassword: security.currentPassword,
                newPassword: security.newPassword
            })
        });

        if (!res.ok) {
            throw new Error("Could not change password, please check your current password.");
        }
    };

    const saveProfilePic = async () => {

        if (initialProfilePicUrl !== null && selectedImgUrl === null) {
            await deleteProfilePic();
            setInitialProfilePicUrl(null);
            return;
        }

        if (!selectedImg) {
            return;
        }

        //creates an object passed via multipart/mixed, more efficient than base64 json
        const formData = new FormData();
        formData.append("profilePicture", selectedImg);
        const res = await customFetch("/api/users/me/profilePicture", {
            method: "PATCH",
            body: formData
        });

        if (!res.ok) {
            throw new Error("Failed to upload profile picture.");
        }
    };

    const handleSave = async (e: React.SyntheticEvent<HTMLFormElement>) => {
        e.preventDefault();
        setProfileError(null);
        setPreferencesError(null);
        setSecurityError(null);
        setGeneralError(null);

        setIsSaving(true);
        let errors = false;
        try {
            if (profileChanged) {
                await saveProfileDetails();
            }
            await saveProfilePic();
        }
        catch (err) {
            const typedError = (err instanceof Error) ? err : new Error("Failed to save profile details");
            setProfileError(typedError.message);
            errors = true;
        }

        try {
            if (preferenceChanged) {
                await savePreferences();
            }
        }
        catch (err) {
            const typedError = (err instanceof Error) ? err : new Error("Failed to save preferences");
            setPreferencesError(typedError.message);
            errors = true;
        }

        try {
            if (securityChanged) {
                await savePassword();
            }
        }
        catch (err) {
            const typedError = (err instanceof Error) ? err : new Error("Failed to change password");
            setSecurityError(typedError.message);
            errors = true;
        }

        setIsSaving(false);

        if (!errors) {
            toast.success("Settings saved successfully", "Saved");
            onClose();
        }
    };

    const revertTheme = () => {
        if (initialPreferencesRef.current) {
            document.documentElement.classList.toggle(
                "dark",
                initialPreferencesRef.current.theme === "dark"
            );
        }
    };

    return {
        profile, updateProfile,
        preferences, updatePreferences,
        security, updateSecurity,
        selectedImgUrl, setSelectedImg, setSelectedImgUrl,
        isLoading, isSaving,
        profileError, preferencesError, securityError, generalError,
        handleSave,
        revertTheme

    };
}


function ProfileSection({ profile, updateProfile, selectedImgUrl, setSelectedImg, setSelectedImgUrl, error }: ProfileParams) {
    const fileInputRef = useRef<HTMLInputElement | null>(null);

    const handleImgChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const file = e.target.files?.[0];
        if (file) {
            const imageTypes = ["image/jpeg", "image/png", "image/webp"];
            if (!imageTypes.includes(file.type)) {
                toast.error("Invalid image format, please use JPEG, PNG, or WebP.", "Upload Failed");
                console.error("Invalid image format:", file.type);

                if (fileInputRef.current) {
                    fileInputRef.current.value = "";
                }

                return;
            }

            //delete old image in memory
            if (selectedImgUrl?.startsWith("blob:")) {
                URL.revokeObjectURL(selectedImgUrl);
            }

            setSelectedImg(file);
            const newUrl = URL.createObjectURL(file);
            setSelectedImgUrl(newUrl);
        }
    };

    const handleRemoveImage = () => {
        if (selectedImgUrl?.startsWith("blob:")) {
            URL.revokeObjectURL(selectedImgUrl);
        }

        setSelectedImg(null);
        setSelectedImgUrl(null);

        if (fileInputRef.current) {
            fileInputRef.current.value = "";
        }
    };

    return (
        <div className="space-y-4">
            <h3 className="font-bold border-b border-border pb-1 text-foreground uppercase tracking-wider text-base">Profile Details</h3>

            <div className="flex flex-col items-center justify-center pb-2">
                <span className="text-xs font-bold uppercase tracking-[1.5px] text-muted-foreground mb-3">Profile Picture</span>
                <div className="group relative flex h-24 w-24 items-center justify-center rounded-full border border-border bg-surface-2 transition-all duration-300">
                    <CircularProfileImage
                        src={selectedImgUrl || undefined}
                        alt={profile.displayName}
                        className="h-full w-full object-cover rounded-full"
                    />
                    <button
                        type="button"
                        onClick={() => fileInputRef.current?.click()}
                        className="absolute inset-0 flex flex-col items-center justify-center rounded-full bg-black/60 opacity-0 group-hover:opacity-100 focus-visible:opacity-100 focus-visible:ring-2 focus-visible:ring-brand focus-visible:ring-offset-2 outline-none transition-opacity duration-200 cursor-pointer text-white"
                        aria-label="Change profile picture"
                    >
                        <ImagePlus size={20} className="mb-1" />
                        <span className="text-[10px] font-semibold uppercase tracking-[0.5px]">Upload</span>
                    </button>
                    <input
                        ref={fileInputRef}
                        type="file"
                        accept="image/*"
                        className="sr-only"
                        onChange={handleImgChange}
                    />
                </div>
                {selectedImgUrl && (
                    <Button
                        type="button"
                        variant="ghost"
                        onClick={handleRemoveImage}
                        className="mt-2.5 h-7 px-3 text-xs text-brand hover:text-brand-2 hover:bg-brand/5 rounded-md"
                    >
                        Remove Picture
                    </Button>
                )}

                <span className="text-[12px] text-muted-foreground mt-2 text-center max-w-[200px]">
                    Supported formats: JPEG, PNG, WebP
                </span>
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Display Name</span>
                <Input value={profile.displayName} onChange={(e) => updateProfile("displayName", e.target.value)} required />
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Bio</span>
                <textarea
                    value={profile.bio}
                    onChange={(e) => updateProfile("bio", e.target.value)}
                    className="w-full rounded-lg border border-input bg-transparent px-3 py-2 text-sm text-foreground outline-none focus-visible:ring-1 focus-visible:ring-ring"
                    rows={2}
                />
            </div>

            <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Sex</span>
                    <DropdownMenu>
                        <DropdownMenuTrigger
                            variant="filter"
                            className="w-full bg-transparent dark:bg-input/30 rounded-lg border-input h-8 py-1 px-2.5"
                        >
                            {profile.sex === "PreferNotToSay" ? "Prefer not to say" : profile.sex}
                        </DropdownMenuTrigger>
                        <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                            {["Male", "Female", "Other", "PreferNotToSay"].map((option) => (
                                <DropdownMenuItem key={option} onClick={() => updateProfile("sex", option)}>
                                    {option === "PreferNotToSay" ? "Prefer not to say" : option}
                                </DropdownMenuItem>
                            ))}
                        </DropdownMenuContent>
                    </DropdownMenu>
                </div>
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Date of Birth</span>
                    <Input type="date" value={profile.dateOfBirth} onChange={(e) => updateProfile("dateOfBirth", e.target.value)} />
                </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Weight ({metricCheck()? 'KG' : 'LB'})</span>
                    <Input type="number" step="0.1" value={profile.weight} onChange={(e) => updateProfile("weight", e.target.value)} />
                </div>
                <div className="flex flex-col gap-1.5">
                    <span className="text-xs font-bold text-muted-foreground">Height ({metricCheck()? 'CM' : 'IN'})</span>
                    <Input type="number" step="0.1" value={profile.height} onChange={(e) => updateProfile("height", e.target.value)} />
                </div>
            </div>

            {error && (
                <div className="bg-destructive/10 border border-destructive/20 text-destructive text-xs p-2.5 rounded-lg mt-2 animate-in fade-in slide-in-from-top-1 duration-200">
                    {error}
                </div>
            )}

        </div>
    );
}

function PreferencesSection({ preferences, updatePreferences, error }: PreferencesParams) {
    return (
        <div className="space-y-4">
            <h3 className="font-bold border-b border-border pb-1 text-foreground uppercase tracking-wider text-base">App Preferences</h3>
            <div className="flex items-center justify-between">
                <span className="text-sm">Theme</span>
                <DropdownMenu>
                    <DropdownMenuTrigger
                        variant="filter"
                        className="w-48 bg-transparent dark:bg-input/30 rounded-lg border-input h-8 py-1 px-2.5"
                    >
                        {preferences.theme === "light" ? "Light Mode" : "Dark Mode"}
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                        <DropdownMenuItem onClick={() => updatePreferences("theme", "light")}>Light Mode</DropdownMenuItem>
                        <DropdownMenuItem onClick={() => updatePreferences("theme", "dark")}>Dark Mode</DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </div>
            <div className="flex items-center justify-between">
                <span className="text-sm">Units</span>
                <DropdownMenu>
                    <DropdownMenuTrigger
                        variant="filter"
                        className="w-48 bg-transparent dark:bg-input/30 rounded-lg border-input h-8 py-1 px-2.5"
                    >
                        {preferences.units === "metric" ? "Metric (kg / cm)" : "Imperial (lbs / in)"}
                    </DropdownMenuTrigger>
                    <DropdownMenuContent className="w-[var(--radix-dropdown-menu-trigger-width)]">
                        <DropdownMenuItem onClick={() => updatePreferences("units", "metric")}>Metric (kg / cm)</DropdownMenuItem>
                        <DropdownMenuItem onClick={() => updatePreferences("units", "imperial")}>Imperial (lbs / in)</DropdownMenuItem>
                    </DropdownMenuContent>
                </DropdownMenu>
            </div>

            {error && (
                <div className="bg-destructive/10 border border-destructive/20 text-destructive text-xs p-2.5 rounded-lg mt-2 animate-in fade-in slide-in-from-top-1 duration-200">
                    {error}
                </div>
            )}

        </div>
    );
}

function SecuritySection({ security, updateSecurity, error }: SecurityParams) {
    const [showCurrent, setShowCurrent] = useState<boolean>(false);
    const [showNew, setShowNew] = useState<boolean>(false);
    const [showConfirm, setShowConfirm] = useState<boolean>(false);

    return (
        <div className="space-y-4">
            <h3 className="font-bold border-b border-border pb-1 text-foreground uppercase tracking-wider text-base">Change Password</h3>

             <p className="text-xs text-muted-foreground -mt-2">
                Passwords need 8 or more characters containing uppercase, lowercase, numbers, and special characters
            </p>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Current Password</span>
                <div className="relative w-full">
                    <Input
                        type={showCurrent ? "text" : "password"}
                        value={security.currentPassword}
                        onChange={(e) => updateSecurity("currentPassword", e.target.value)}
                        className="pr-11"
                    />
                    <Button
                        type="button" variant="password" size="icon"
                        onClick={() => setShowCurrent(!showCurrent)}
                        className="absolute right-1 top-1/2 -translate-y-1/2"
                    >
                        {showCurrent ? <Eye size={16} /> : <EyeOff size={16} />}
                    </Button>
                </div>
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">New Password</span>
                <div className="relative w-full">
                    <Input
                        type={showNew ? "text" : "password"}
                        value={security.newPassword}
                        onChange={(e) => updateSecurity("newPassword", e.target.value)}
                        className="pr-11"
                    />
                    <Button
                        type="button" variant="password" size="icon"
                        onClick={() => setShowNew(!showNew)}
                        className="absolute right-1 top-1/2 -translate-y-1/2"
                    >
                        {showNew ? <Eye size={16} /> : <EyeOff size={16} />}
                    </Button>
                </div>
            </div>

            <div className="flex flex-col gap-1.5">
                <span className="text-xs font-bold text-muted-foreground">Confirm New Password</span>
                <div className="relative w-full">
                    <Input
                        type={showConfirm ? "text" : "password"}
                        value={security.confirmPassword}
                        onChange={(e) => updateSecurity("confirmPassword", e.target.value)}
                        className="pr-11"
                    />
                    <Button
                        type="button" variant="password" size="icon"
                        onClick={() => setShowConfirm(!showConfirm)}
                        className="absolute right-1 top-1/2 -translate-y-1/2"
                    >
                        {showConfirm ? <Eye size={16} /> : <EyeOff size={16} />}
                    </Button>
                </div>
            </div>

            {error && (
                <div className="bg-destructive/10 border border-destructive/20 text-destructive text-xs p-2.5 rounded-lg mt-2 animate-in fade-in slide-in-from-top-1 duration-200">
                    {error}
                </div>
            )}

        </div>
    );
}

export function UserSettingsPopup({ isOpen, onClose }: UserSettingsPopupProps) {
    const { logout } = useAuth();
    const [isLogoutConfirmOpen, setIsLogoutConfirmOpen] = useState(false);

    useEffect(() => {
        if (isOpen) {
            document.body.style.overflow = "hidden";
        } else {
            document.body.style.overflow = "";
        }

        return () => {
            document.body.style.overflow = "";
        };
    }, [isOpen]);

    const {
        profile, updateProfile,
        preferences, updatePreferences,
        security, updateSecurity,
        selectedImgUrl, setSelectedImg, setSelectedImgUrl,
        isLoading, isSaving,
        profileError, preferencesError, securityError, generalError,
        handleSave,
        revertTheme
    } = useSettingsLogic(isOpen, onClose);

    if (!isOpen) {
        return null;
    }

    const handleClosePopup = () => {
        // upload img but cancle
        if (selectedImgUrl?.startsWith("blob:")) {
            URL.revokeObjectURL(selectedImgUrl);
        }

        setSelectedImg(null);
        setSelectedImgUrl(null);

        revertTheme();

        onClose();
    };

    return (
        <div className="fixed top-20 inset-x-0 bottom-0 z-50 flex items-center justify-center bg-black/50 backdrop-blur-sm p-4">
            <button
                type="button"
                className="absolute inset-0 block w-full cursor-default outline-none bg-black/50 backdrop-blur-sm"
                aria-label="Close settings"
                onClick={handleClosePopup}
                tabIndex={-1}
            />
            
            <div className="relative z-10 w-full max-w-lg bg-surface border border-border rounded-xl shadow-lg flex flex-col max-h-[85vh] overflow-hidden animate-in fade-in zoom-in-95 duration-200">

                <div className="flex items-center justify-between border-b border-border p-4">
                    <h2 className="text-xl font-bold font-display uppercase tracking-wider text-foreground">Settings</h2>
                    <button onClick={handleClosePopup} className="text-muted-foreground hover:text-foreground cursor-pointer">
                        <X size={20} />
                    </button>
                </div>

                {isLoading ? (
                    <div className="p-8 text-center text-muted-foreground flex flex-col items-center justify-center gap-2">
                        <Loader2 className="animate-spin text-brand" />
                        <span>Loading settings...</span>
                    </div>
                ) : (
                    <form onSubmit={handleSave} className="flex-1 overflow-y-auto p-5 space-y-6">

                        {generalError && (
                            <div className="bg-destructive/10 border border-destructive/20 text-destructive text-sm p-3 rounded-lg">
                                {generalError}
                            </div>
                        )}

                        <ProfileSection
                            profile={profile} updateProfile={updateProfile}
                            selectedImgUrl={selectedImgUrl}
                            setSelectedImg={setSelectedImg}
                            setSelectedImgUrl={setSelectedImgUrl}
                            error={profileError}
                        />

                        <PreferencesSection
                            preferences={preferences} updatePreferences={updatePreferences}
                            error={preferencesError}
                        />

                        <SecuritySection
                            security={security} updateSecurity={updateSecurity}
                            error={securityError}
                        />

                        {/* logout */}
                        <div className="space-y-4 pt-2">
                            <h3 className="font-bold border-b border-border pb-1 text-foreground uppercase tracking-wider text-base">Account Management</h3>
                            <div className="flex items-center justify-between gap-4">
                                <span className="text-sm text-muted-foreground focus-visible:outline-brand">Log out of your current session on this device.</span>
                                <Button
                                    type="button"
                                    variant="outline"
                                    className="w-48 text-destructive border-destructive hover:bg-destructive/10 hover:text-destructive transition-colors shrink-0"
                                    onClick={() => setIsLogoutConfirmOpen(true)}
                                >
                                    Log Out
                                </Button>
                            </div>
                        </div>

                        <div className="flex justify-end gap-3 pt-4 border-t border-border">
                            <Button type="button" variant="secondary" onClick={handleClosePopup} disabled={isSaving}>Cancel</Button>
                            <Button type="submit" disabled={isSaving}>{isSaving ? "Saving..." : "Save Changes"}</Button>
                        </div>
                    </form>
                )}
            </div>

            <ConfirmDialog
                isOpen={isLogoutConfirmOpen}
                onClose={() => setIsLogoutConfirmOpen(false)}
                onConfirm={() => {
                    setIsLogoutConfirmOpen(false);
                    onClose();
                    logout();
                }}
                title="Log Out"
                description="Are you sure you want to log out of your account?"
                confirmText="Log Out"
                cancelText="Cancel"
                variant="danger"
            />
        </div>

    );
}