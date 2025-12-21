import re

file_path = r'c:\SR3H_MACRO\Services\MacroFortActivationService.cs'

with open(file_path, 'r', encoding='utf-8') as f:
    content = f.read()

# Add _codeService field after _credentialsManager
content = re.sub(
    r'(private readonly MacroFortSecureCredentialsManager _credentialsManager;)',
    r'\1\n        private readonly SubscriptionCodeService _codeService;',
    content
)

# Initialize _codeService in constructor
content = re.sub(
    r'(_credentialsManager = new MacroFortSecureCredentialsManager\(\);)',
    r'\1\n            _codeService = new SubscriptionCodeService();',
    content
)

with open(file_path, 'w', encoding='utf-8') as f:
    f.write(content)

print('✓ تم تحديث MacroFortActivationService')
